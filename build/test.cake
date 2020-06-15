#region Tests

Task("UnitTest")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledUnitTests, "Unit tests were disabled.")
    .IsDependentOn("Build")
    .Does<BuildParameters>((parameters) =>
{
    var frameworks = new[] { parameters.CoreFxVersion31, parameters.FullFxVersion472 };
    var testResultsPath = parameters.Paths.Directories.TestResultsOutput;

    foreach(var framework in frameworks)
    {
        // run using dotnet test
        var actions = new List<Action>();
        var projects = GetFiles("./src/**/*.Tests.csproj");
        foreach(var project in projects)
        {
            actions.Add(() =>
            {
                var projectName = $"{project.GetFilenameWithoutExtension()}.{framework}";
                var settings = new DotNetCoreTestSettings {
                    Framework = framework,
                    NoBuild = true,
                    NoRestore = true,
                    Configuration = parameters.Configuration,
                };

                if (!parameters.IsRunningOnMacOS) {
                    settings.TestAdapterPath = new DirectoryPath(".");
                    var resultsPath = MakeAbsolute(testResultsPath.CombineWithFilePath($"{projectName}.results.xml"));
                    settings.Logger = $"nunit;LogFilePath={resultsPath}";
                }

                var coverletSettings = new CoverletSettings {
                    CollectCoverage = true,
                    CoverletOutputFormat = CoverletOutputFormat.cobertura,
                    CoverletOutputDirectory = testResultsPath,
                    CoverletOutputName = $"{projectName}.coverage.xml",
                    Exclude = new List<string> { "[GitVersion*.Tests]*", "[GitVersionTask.MsBuild]*" }
                };

                if (IsRunningOnUnix() && string.Equals(framework, parameters.FullFxVersion472))
                {
                    settings.Filter = "TestCategory!=NoMono";
                }

                DotNetCoreTest(project.FullPath, settings, coverletSettings);
            });
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = -1,
            CancellationToken = default
        };

        Parallel.Invoke(options, actions.ToArray());
    }
})
.ReportError(exception =>
{
    var error = (exception as AggregateException).InnerExceptions[0];
    Error(error.Dump());
})
.Finally(() =>
{
    var parameters = Context.Data.Get<BuildParameters>();
    var testResultsFiles = GetFiles(parameters.Paths.Directories.TestResultsOutput + "/*.results.xml");
    if (parameters.IsRunningOnAzurePipeline)
    {
        if (testResultsFiles.Any()) {
            var data = new TFBuildPublishTestResultsData {
                TestResultsFiles = testResultsFiles.ToArray(),
                Platform = Context.Environment.Platform.Family.ToString(),
                TestRunner = TFTestRunnerType.NUnit
            };
            TFBuild.Commands.PublishTestResults(data);
        }
    }
});

Task("Publish-Coverage")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows, "Publish-Coverage works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,      "Publish-Coverage works only on Releasing CI.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-Coverage works only for releases.")
    .IsDependentOn("UnitTest")
    .Does<BuildParameters>((parameters) =>
{
    var coverageFiles = GetFiles(parameters.Paths.Directories.TestResultsOutput + "/*.coverage.*.xml");

    var token = parameters.Credentials.CodeCov.Token;
    if(string.IsNullOrEmpty(token)) {
        throw new InvalidOperationException("Could not resolve CodeCov token.");
    }

    foreach (var coverageFile in coverageFiles) {
        Codecov(new CodecovSettings {
            Files = new [] { coverageFile.ToString() },
            Token = token
        });
    }
});

#endregion
