using Cake.Common.Build.AzurePipelines.Data;
using Cake.Common.Tools.DotNet.Test;
using Cake.Coverlet;
using Cake.Incubator.LoggingExtensions;
using Common.Utilities;

namespace Build.Tasks;

[TaskName(nameof(UnitTest))]
[TaskDescription("Run the unit tests")]
[TaskArgument(Arguments.DotnetTarget, Constants.NetVersion60, Constants.CoreFxVersion31)]
[IsDependentOn(typeof(Build))]
public class UnitTest : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context) => context.EnabledUnitTests;

    public override void Run(BuildContext context)
    {
        var dotnetTarget = context.Argument(Arguments.DotnetTarget, string.Empty);
        var frameworks = new[] { Constants.CoreFxVersion31, Constants.NetVersion60 };
        if (!string.IsNullOrWhiteSpace(dotnetTarget))
        {
            if (!frameworks.Contains(dotnetTarget, StringComparer.OrdinalIgnoreCase))
            {
                throw new Exception($"Dotnet Target {dotnetTarget} is not supported at the moment");
            }
            frameworks = new[] { dotnetTarget };
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
        if (!context.IsAzurePipelineBuild || !testResultsFiles.Any()) return;

        var data = new AzurePipelinesPublishTestResultsData
        {
            TestResultsFiles = testResultsFiles.ToArray(),
            Platform = context.Environment.Platform.Family.ToString(),
            TestRunner = AzurePipelinesTestRunnerType.NUnit
        };
        context.BuildSystem().AzurePipelines.Commands.PublishTestResults(data);
    }

    private static void TestProjectForTarget(BuildContext context, FilePath project, string framework)
    {
        var testResultsPath = Paths.TestOutput;
        var projectName = $"{project.GetFilenameWithoutExtension()}.{framework}";
        var settings = new DotNetTestSettings
        {
            Framework = framework,
            NoBuild = true,
            NoRestore = true,
            Configuration = context.MsBuildConfiguration,
        };

        if (!context.IsRunningOnMacOs())
        {
            settings.TestAdapterPath = new DirectoryPath(".");
            var resultsPath = context.MakeAbsolute(testResultsPath.CombineWithFilePath($"{projectName}.results.xml"));
            settings.Loggers = new[] { $"nunit;LogFilePath={resultsPath}" };
        }

        var coverletSettings = new CoverletSettings
        {
            CollectCoverage = true,
            CoverletOutputFormat = CoverletOutputFormat.cobertura,
            CoverletOutputDirectory = testResultsPath,
            CoverletOutputName = $"{projectName}.coverage.xml",
            Exclude = new List<string> { "[GitVersion*.Tests]*", "[GitTools.Testing]*" }
        };

        context.DotNetTest(project.FullPath, settings, coverletSettings);
    }
}
