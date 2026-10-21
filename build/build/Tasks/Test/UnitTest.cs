using Cake.Common.Tools.DotNet.Execute;
using Cake.Common.Tools.DotNet.Restore;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(UnitTest))]
[TaskDescription("Run the unit tests")]
[DotnetArgument]
[TaskArgument(Arguments.TestResults)]
[TaskArgument(Arguments.TestProjects)]
[TaskArgument(Arguments.TestFilter)]
[IsDependentOn(typeof(Clean))]
public class UnitTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.EnabledUnitTests;

    public override void Run(BuildContext context)
    {
        var frameworks = GetFrameworks(context);
        var projects = context.GetFiles($"{Paths.Src}/**/*.Tests.csproj").OrderBy(project => project.FullPath).ToArray();
        if (projects.Length == 0)
        {
            throw new CakeException("No test projects were found.");
        }

        var requested = context.Argument(Arguments.TestProjects, string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (requested.Length > 0)
        {
            var unknown = requested.Except(projects.Select(project => project.GetFilenameWithoutExtension().FullPath), StringComparer.OrdinalIgnoreCase).ToArray();
            if (unknown.Length > 0)
            {
                throw new CakeException($"Unknown test projects: {string.Join(", ", unknown)}.");
            }
            projects = [.. projects.Where(project => requested.Contains(project.GetFilenameWithoutExtension().FullPath, StringComparer.OrdinalIgnoreCase))];
        }

        // Build each selected target below with real source paths. An upfront solution
        // build compiles both frameworks with CI paths, then recompiles them for tests.
        context.DotNetRestore("./src/GitVersion.slnx", new DotNetRestoreSettings
        {
            Verbosity = DotNetVerbosity.Minimal,
            Sources = [Constants.NugetOrgUrl],
            MSBuildSettings = context.MsBuildSettings
        });

        var failures = new List<string>();
        foreach (var framework in frameworks)
        {
            foreach (var project in projects)
            {
                var exitCode = TestProjectForTarget(context, project, framework);
                if (exitCode != 0)
                {
                    // MTP exit code 2 means tests failed. Other codes indicate an incomplete run.
                    var outcome = exitCode == 2 ? "tests failed" : "test run incomplete";
                    var failure = $"{project.GetFilenameWithoutExtension()} / net{framework}: {outcome} (exit {exitCode})";
                    context.Error(failure);
                    failures.Add(failure);
                }
            }
        }

        if (failures.Count > 0)
        {
            throw new CakeException(string.Join(Environment.NewLine, failures));
        }
    }

    private static string[] GetFrameworks(BuildContext context)
    {
        var dotnetVersion = context.Argument(Arguments.DotnetVersion, string.Empty);
        var frameworks = Constants.DotnetVersions;
        if (!string.IsNullOrWhiteSpace(dotnetVersion))
        {
            if (string.Equals(dotnetVersion, "lts-latest", StringComparison.OrdinalIgnoreCase))
            {
                dotnetVersion = Constants.DotnetLtsLatest;
            }
            if (!frameworks.Contains(dotnetVersion, StringComparer.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Dotnet Target {dotnetVersion} is not supported at the moment");
            }
            frameworks = [dotnetVersion];
        }

        return frameworks;
    }

    private static int TestProjectForTarget(BuildContext context, FilePath project, string framework)
    {
        var settings = new DotNetBuildSettings
        {
            Framework = $"net{framework}",
            // The solution is restored once; build with real paths for snapshots and annotations.
            NoRestore = true,
            Configuration = context.MsBuildConfiguration,
            MSBuildSettings = new()
        };
        settings.MSBuildSettings.SetContinuousIntegrationBuild(false);
        context.DotNetBuild(project.FullPath, settings);

        var query = new DotNetMSBuildSettings { NoLogo = true };
        query.WithProperty("Configuration", context.MsBuildConfiguration);
        query.WithProperty("TargetFramework", $"net{framework}");
        query.SetContinuousIntegrationBuild(false);
        query.GetProperties.Add("TargetPath");
        var output = new List<string>();
        context.DotNetMSBuild(project.FullPath, query, lines => output.AddRange(lines));
        var targetPath = output.Single(line => !string.IsNullOrWhiteSpace(line)).Trim();
        if (!context.FileExists(targetPath))
        {
            throw new CakeException($"Test assembly not found for {project} / net{framework}: {targetPath}");
        }

        var attempt = context.EnvironmentVariable("GITHUB_RUN_ATTEMPT") ?? "local";
        var root = new DirectoryPath(context.Argument(Arguments.TestResults,
            Paths.TestOutput.Combine($"attempt-{attempt}").FullPath));
        var resultsDirectory = context.MakeAbsolute(root.Combine(project.GetFilenameWithoutExtension().FullPath).Combine($"net{framework}"));
        context.CleanDirectory(resultsDirectory);
        // CI publishes coverage from the LTS target; other targets validate runtime compatibility.
        var collectCoverage = framework == Constants.DotnetLtsLatest;
        var args = new ProcessArgumentBuilder().AppendArguments(resultsDirectory, collectCoverage);
        var filter = context.Argument(Arguments.TestFilter, string.Empty);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            args.Append("--filter").AppendQuoted(filter);
            // NUnit otherwise drops name filters above 2000 cases and executes
            // excluded tests without including their outcomes in MTP's reports.
            args.Append("--settings").AppendQuoted(context.MakeAbsolute(Paths.Build.CombineWithFilePath("test.runsettings")).FullPath);
        }
        if (context.BuildSystem().IsRunningOnGitHubActions)
        {
            args = args.AppendGitHubArguments();
        }

        if (project.GetFilenameWithoutExtension().FullPath == "GitVersion.App.Tests")
        {
            args.Append("--diagnostic")
                .Append("--diagnostic-output-directory").AppendQuoted(resultsDirectory.FullPath)
                .Append("--diagnostic-verbosity").Append("Trace");
        }

        // Direct execution preserves annotation commands that SDK 10.0.401's dotnet test suppressed.
        // Collect process failures so subsequent projects still run; build/start exceptions propagate.
        var exitCode = 0;
        context.DotNetExecute(targetPath, args, new DotNetExecuteSettings
        {
            HandleExitCode = code =>
            {
                exitCode = code;
                return true;
            }
        });
        // Coverlet can report an instrumentation error while MTP still returns success.
        // A green test job must include machine-readable results and coverage when enabled.
        if (exitCode == 0 && (!context.FileExists(resultsDirectory.CombineWithFilePath("results.xml"))
            || (collectCoverage && !context.GetFiles($"{resultsDirectory.FullPath}/*cobertura*.xml").Any())))
        {
            throw new CakeException($"Missing JUnit or coverage report for {project} / net{framework}.");
        }
        return exitCode;
    }
}
