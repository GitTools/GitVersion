using Cake.Common.Build.AzurePipelines.Data;
using Cake.Common.Tools.DotNet.Test;
using Cake.Coverlet;
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

    public override void Finally(BuildContext context)
    {
        var testResultsFiles = context.GetFiles($"{Paths.TestOutput}/*.results.xml");
        if (!context.IsAzurePipelineBuild || testResultsFiles.Count == 0) return;

        var data = new AzurePipelinesPublishTestResultsData
        {
            TestResultsFiles = testResultsFiles.ToArray(),
            Platform = context.Platform.ToString(),
            TestRunner = AzurePipelinesTestRunnerType.JUnit
        };
        context.BuildSystem().AzurePipelines.Commands.PublishTestResults(data);
    }

    private static void TestProjectForTarget(BuildContext context, FilePath project, string framework)
    {
        var testResultsPath = Paths.TestOutput;
        var projectName = $"{project.GetFilenameWithoutExtension()}.net{framework}";
        var settings = new DotNetTestSettings
        {
            Framework = $"net{framework}",
            NoBuild = true,
            NoRestore = true,
            Configuration = context.MsBuildConfiguration,
            TestAdapterPath = new(".")
        };

        var resultsPath = context.MakeAbsolute(testResultsPath.CombineWithFilePath($"{projectName}.results.xml"));
        settings.Loggers = [$"junit;LogFilePath={resultsPath}"];

        var coverletSettings = new CoverletSettings
        {
            CollectCoverage = true,
            CoverletOutputFormat = CoverletOutputFormat.cobertura,
            CoverletOutputDirectory = testResultsPath,
            CoverletOutputName = $"{projectName}.coverage.xml",
            Exclude = ["[GitVersion*.Tests]*", "[GitTools.Testing]*"]
        };

        context.DotNetTest(project.FullPath, settings, coverletSettings);
    }
}
