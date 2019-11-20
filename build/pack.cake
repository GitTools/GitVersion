#region Build

Task("Clean")
    .Does<BuildParameters>((parameters) =>
{
    Information("Cleaning directories..");

    CleanDirectories("./src/**/bin/" + parameters.Configuration);
    CleanDirectories("./src/**/obj");
    CleanDirectories("./src/GitVersionVsixTask/scripts/**");

    DeleteFiles("src/GitVersionVsixTask/*.vsix");
    DeleteFiles("src/GitVersionRubyGem/*.gem");

    CleanDirectories(parameters.Paths.Directories.ToClean);
});

Task("Build")
    .IsDependentOn("Clean")
    .Does<BuildParameters>((parameters) =>
{
    // build .Net code
    Build(parameters);
});

Task("Build-Vsix")
    .IsDependentOn("Build")
    .Does<BuildParameters>((parameters) =>
{
    var workDir = "./src/GitVersionVsixTask";
    // build typescript code
    NpmSet(new NpmSetSettings             { WorkingDirectory = workDir, LogLevel = NpmLogLevel.Silent, Key = "progress", Value = "false" });
    NpmInstall(new NpmInstallSettings     { WorkingDirectory = workDir, LogLevel = NpmLogLevel.Silent });
    NpmRunScript(new NpmRunScriptSettings { WorkingDirectory = workDir, LogLevel = NpmLogLevel.Silent, ScriptName = "build" });
});

#endregion

#region Tests

Task("UnitTest")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledUnitTests, "Unit tests were disabled.")
    .IsDependentOn("Build")
    .Does<BuildParameters>((parameters) =>
{
    var frameworks = new[] { parameters.CoreFxVersion21, parameters.CoreFxVersion30, parameters.FullFxVersion472 };
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
                    CoverletOutputFormat = CoverletOutputFormat.opencover,
                    CoverletOutputDirectory = testResultsPath,
                    CoverletOutputName = $"{projectName}.coverage.xml"
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
                TestRunner = TFTestRunnerType.NUnit
            };
            TFBuild.Commands.PublishTestResults(data);
        }
    }
});

Task("UnitTest-Vsix")
    .IsDependentOn("Build-Vsix")
    .Does<BuildParameters>((parameters) =>
{
    var workDir = "./src/GitVersionVsixTask";
    var testResultsPath = parameters.Paths.Directories.TestResultsOutput;
    var npmSettings = new NpmRunScriptSettings { WorkingDirectory = workDir, LogLevel = NpmLogLevel.Silent, ScriptName = "test" };
    var vsixResultsPath = MakeAbsolute(testResultsPath.CombineWithFilePath("vsix.results.xml"));
    // npmSettings.Arguments.Add($"--reporter mocha-xunit-reporter --reporter-options mochaFile={vsixResultsPath}");
    NpmRunScript(npmSettings);
});

Task("Publish-Coverage")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,       "Publish-Coverage works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Publish-Coverage works only on AzurePipeline.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-Coverage works only for releases.")
    .IsDependentOn("UnitTest")
    .IsDependentOn("UnitTest-Vsix")
    .Does<BuildParameters>((parameters) =>
{
    var coverageFiles = GetFiles(parameters.Paths.Directories.TestResultsOutput + "/*.coverage.xml");

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

#region Pack
Task("Pack-Prepare")
    .IsDependentOn("Build")
    .Does<BuildParameters>((parameters) =>
{
    // publish single file for all native runtimes (self contained)
    foreach(var runtime in parameters.NativeRuntimes)
    {
        var runtimeName = runtime.Value;

        var settings = new DotNetCorePublishSettings
        {
            Framework = parameters.CoreFxVersion30,
            Runtime = runtimeName,
            NoRestore = false,
            Configuration = parameters.Configuration,
            OutputDirectory = parameters.Paths.Directories.Native.Combine(runtimeName),
            MSBuildSettings = parameters.MSBuildSettings,
        };

        settings.ArgumentCustomization =
            arg => arg
            .Append("/p:PublishSingleFile=true")
            .Append("/p:PublishTrimmed=true")
            .Append("/p:IncludeSymbolsInSingleFile=true");

        DotNetCorePublish("./src/GitVersionExe/GitVersionExe.csproj", settings);
    }

    var frameworks = new[] { parameters.CoreFxVersion21, parameters.CoreFxVersion30, parameters.FullFxVersion472 };

    // publish Framework-dependent deployment
    foreach(var framework in frameworks)
    {
        var settings = new DotNetCorePublishSettings
        {
            Framework = framework,
            NoRestore = false,
            Configuration = parameters.Configuration,
            OutputDirectory = parameters.Paths.Directories.ArtifactsBin.Combine(framework),
            MSBuildSettings = parameters.MSBuildSettings,
        };

        DotNetCorePublish("./src/GitVersionExe/GitVersionExe.csproj", settings);
    }

    frameworks = new[] { parameters.CoreFxVersion21, parameters.FullFxVersion472 };

    // MsBuild Task
    foreach(var framework in frameworks)
    {
        DotNetCorePublish("./src/GitVersionTask/GitVersionTask.csproj", new DotNetCorePublishSettings
        {
            Framework = framework,
            Configuration = parameters.Configuration,
            MSBuildSettings = parameters.MSBuildSettings
        });
    }

    var sourceDir = parameters.Paths.Directories.Native.Combine(parameters.NativeRuntimes[PlatformFamily.Windows]);
    var sourceFiles = GetFiles(sourceDir + "/*.*");

    // RubyGem
    var gemDir = new DirectoryPath("./src/GitVersionRubyGem/bin");
    EnsureDirectoryExists(gemDir);
    CopyFiles(sourceFiles, gemDir);

    // Cmdline and Portable
    var cmdlineDir = parameters.Paths.Directories.ArtifactsBinCmdline.Combine("tools");
    var portableDir = parameters.Paths.Directories.ArtifactsBinPortable.Combine("tools");

    EnsureDirectoryExists(cmdlineDir);
    EnsureDirectoryExists(portableDir);

    CopyFiles(sourceFiles, cmdlineDir);

    sourceFiles += GetFiles("./nuspec/*.ps1") + GetFiles("./nuspec/*.txt");
    CopyFiles(sourceFiles, portableDir);
});

Task("Pack-Vsix")
    .IsDependentOn("Build-Vsix")
    .Does<BuildParameters>((parameters) =>
{
    var workDir = "./src/GitVersionVsixTask";
    var idSuffix    = parameters.IsStableRelease() ? "" : "-preview";
    var titleSuffix = parameters.IsStableRelease() ? "" : " (Preview)";
    var visibility  = parameters.IsStableRelease() ? "Public" : "Preview";
    var taskId      = parameters.IsStableRelease() ? "bab30d5c-39f3-49b0-a7db-9a5da6676eaa" : "dd065e3b-6aef-46af-845c-520195836b35";

    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.json"), "$idSuffix$", idSuffix);
    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.json"), "$titleSuffix$", titleSuffix);
    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.json"), "$visibility$", visibility);
    ReplaceTextInFile(new FilePath(workDir + "/GitVersionTask/task.json"), "$titleSuffix$", titleSuffix);

    // update version number
    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.json"), "$version$", parameters.Version.VsixVersion);
    UpdateTaskVersion(new FilePath(workDir + "/GitVersionTask/task.json"), taskId, parameters.Version.GitVersion);

    // build and pack
    var settings = new TfxExtensionCreateSettings
    {
        ToolPath = workDir + "/node_modules/.bin/" + (parameters.IsRunningOnWindows ? "tfx.cmd" : "tfx"),
        WorkingDirectory = workDir,
        OutputPath = parameters.Paths.Directories.BuildArtifact
    };

    settings.ManifestGlobs = new List<string>(){ "vss-extension.json" };
    TfxExtensionCreate(settings);
});

Task("Pack-Gem")
    .IsDependentOn("Pack-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    var workDir = "./src/GitVersionRubyGem";

    var gemspecFile = new FilePath(workDir + "/gitversion.gemspec");
    // update version number
    ReplaceTextInFile(gemspecFile, "$version$", parameters.Version.GemVersion);

    var toolPath = FindToolInPath(IsRunningOnWindows() ? "gem.cmd" : "gem");
    GemBuild(gemspecFile, new Cake.Gem.Build.GemBuildSettings()
    {
        WorkingDirectory = workDir,
        ToolPath = toolPath
    });

    CopyFiles(workDir + "/*.gem", parameters.Paths.Directories.BuildArtifact);
});

Task("Pack-Nuget")
    .IsDependentOn("Pack-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    foreach(var package in parameters.Packages.Nuget)
    {
        if (FileExists(package.NuspecPath)) {
            var artifactPath = MakeAbsolute(parameters.PackagesBuildMap[package.Id]).FullPath;

            var nugetSettings = new NuGetPackSettings
            {
                Version = parameters.Version.NugetVersion,
                NoPackageAnalysis = true,
                OutputDirectory = parameters.Paths.Directories.NugetRoot,
                Files = GetFiles(artifactPath + "/**/*.*")
                        .Select(file => new NuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })
                        .ToArray()
            };

            NuGetPack(package.NuspecPath, nugetSettings);
        }
    }

    var settings = new DotNetCorePackSettings
    {
        Configuration = parameters.Configuration,
        OutputDirectory = parameters.Paths.Directories.NugetRoot,
        MSBuildSettings = parameters.MSBuildSettings
    };

    // GitVersionTask, & global tool
    DotNetCorePack("./src/GitVersionTask", settings);

    settings.ArgumentCustomization = arg => arg.Append("/p:PackAsTool=true");
    DotNetCorePack("./src/GitVersionExe/GitVersionExe.csproj", settings);
});

Task("Pack-Chocolatey")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,  "Pack-Chocolatey works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsMainBranch && !parameters.IsPullRequest, "Pack-Chocolatey works only for main branch.")
    .IsDependentOn("Pack-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    foreach(var package in parameters.Packages.Chocolatey)
    {
        if (FileExists(package.NuspecPath)) {
            var artifactPath = MakeAbsolute(parameters.PackagesBuildMap[package.Id]).FullPath;

            var chocolateySettings = new ChocolateyPackSettings
            {
                LimitOutput = true,
                Version = parameters.Version.SemVersion,
                OutputDirectory = parameters.Paths.Directories.NugetRoot,
                Files = GetFiles(artifactPath + "/**/*.*")
                        .Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })
                        .ToArray()
            };
            ChocolateyPack(package.NuspecPath, chocolateySettings);
        }
    }
});

Task("Zip-Files")
    .IsDependentOn("Pack-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    foreach(var runtime in parameters.NativeRuntimes)
    {
        var sourceDir = parameters.Paths.Directories.Native.Combine(runtime.Value);
        var fileName = $"gitversion-{runtime.Key}-{parameters.Version.SemVersion}.tar.gz".ToLower();
        var tarFile = parameters.Paths.Directories.Artifacts.CombineWithFilePath(fileName);
        GZipCompress(sourceDir, tarFile);
    }
});
#endregion
