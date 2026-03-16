using Cake.Common.Tools.DotNet.Test;
using Cake.Incubator.LoggingExtensions;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(UnitTest))]
[TaskDescription("Run the unit tests")]
[DotnetArgument]
[IsDependentOn(typeof(Build))]
public class UnitTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.EnabledUnitTests;

    public override void Run(BuildContext context)
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
                throw new Exception($"Dotnet Target {dotnetVersion} is not supported at the moment");
            }
            frameworks = [dotnetVersion];
        }

        foreach (var framework in frameworks)
        {
            // run using dotnet test
            var projects = context.GetFiles($"{Paths.Src}/**/*.Tests.csproj");
            foreach (var project in projects)
            {
                TestProjectForTarget(context, project, framework);
            }
        }
    }

    public override void OnError(Exception exception, BuildContext context)
    {
        var error = (exception as AggregateException)?.InnerExceptions[0];
        context.Error(error.Dump());
        throw exception;
    }

    private static void TestProjectForTarget(BuildContext context, FilePath project, string framework)
    {
        var testResultsPath = Paths.TestOutput;
        var projectName = $"{project.GetFilenameWithoutExtension()}";
        var settings = new DotNetTestSettings
        {
            PathType = DotNetTestPathType.Project,
            Framework = $"net{framework}",
            NoBuild = false,
            NoRestore = false,
            Configuration = context.MsBuildConfiguration,
            MSBuildSettings = new()
        };
        settings.MSBuildSettings.SetContinuousIntegrationBuild(false);

        var resultsDirectory = context.MakeAbsolute(testResultsPath.Combine(projectName));

        settings.WithArgumentCustomization(args => args
            .Append("--report-spekt-junit")
            .Append("--report-spekt-junit-filename").AppendQuoted(resultsDirectory.CombineWithFilePath("results.xml").FullPath)
            .Append("--results-directory").AppendQuoted(resultsDirectory.FullPath)
            .Append("--coverlet")
            .Append("--coverlet-output-format").AppendQuoted("cobertura")
            .Append("--coverlet-exclude").AppendQuoted("[GitVersion*.Tests]*")
            .Append("--coverlet-exclude").AppendQuoted("[GitVersion.Testing]*")
        );

        context.DotNetTest(project.FullPath, settings);
    }
}
