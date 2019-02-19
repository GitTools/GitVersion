// Install modules
#module nuget:?package=Cake.DotNetTool.Module&version=0.1.0

// Install addins.
#addin "nuget:?package=Cake.Gitter&version=0.9.0"
#addin "nuget:?package=Cake.Docker&version=0.9.6"
#addin "nuget:?package=Cake.Npm&version=0.15.0"
#addin "nuget:?package=Cake.Incubator&version=3.0.0"
#addin "nuget:?package=Cake.Json&version=3.0.0"
#addin "nuget:?package=Cake.Tfx&version=0.8.0"
#addin "nuget:?package=Cake.Gem&version=0.7.0"
#addin "nuget:?package=Cake.Coverlet&version=2.2.1"
#addin "nuget:?package=Cake.Codecov&version=0.5.0"
#addin "nuget:?package=Newtonsoft.Json&version=9.0.1"

// Install tools.
#tool "nuget:?package=GitReleaseManager&version=0.7.1"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.9.0"
#tool "nuget:?package=GitReleaseNotes&version=0.7.1"
#tool "nuget:?package=ILRepack&version=2.0.16"
#tool "nuget:?package=Codecov&version=1.1.0"
#tool "nuget:?package=nuget.commandline&version=4.9.2"

// Install .NET Core Global tools.
#tool "dotnet:?package=GitReleaseManager.Tool&version=0.8.0"

// Load other scripts.
#load "./build/parameters.cake"
#load "./build/utils.cake"

//////////////////////////////////////////////////////////////////////
// PARAMETERS
//////////////////////////////////////////////////////////////////////
bool publishingError = false;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup<BuildParameters>(context =>
{
    var parameters = BuildParameters.GetParameters(Context);
    Build(parameters.Configuration);
    var gitVersion = GetVersion(parameters);
    parameters.Initialize(context, gitVersion);

    // Increase verbosity?
    if (parameters.IsMainBranch && (context.Log.Verbosity != Verbosity.Diagnostic)) {
        Information("Increasing verbosity to diagnostic.");
        context.Log.Verbosity = Verbosity.Diagnostic;
    }

    Information("Building version {0} of GitVersion ({1}, {2})",
        parameters.Version.SemVersion,
        parameters.Configuration,
        parameters.Target);

    Information("Repository info : IsMainRepo {0}, IsMainBranch {1}, IsTagged: {2}, IsPullRequest: {3}",
        parameters.IsMainRepo,
        parameters.IsMainBranch,
        parameters.IsTagged,
        parameters.IsPullRequest);

    return parameters;
});

Teardown<BuildParameters>((context, parameters) =>
{
    Information("Starting Teardown...");

    Information("Repository info : IsMainRepo {0}, IsMainBranch {1}, IsTagged: {2}, IsPullRequest: {3}",
        parameters.IsMainRepo,
        parameters.IsMainBranch,
        parameters.IsTagged,
        parameters.IsPullRequest);

    if(context.Successful)
    {
        // if(parameters.ShouldPublish)
        // {
        //     if(parameters.CanPostToGitter)
        //     {
        //         var message = "@/all Version " + parameters.Version.SemVersion + " of the GitVersion has just been released, https://www.nuget.org/packages/GitVersion.";

        //         var postMessageResult = Gitter.Chat.PostMessage(
        //             message: message,
        //             messageSettings: new GitterChatMessageSettings { Token = parameters.Gitter.Token, RoomId = parameters.Gitter.RoomId}
        //         );

        //         if (postMessageResult.Ok)
        //         {
        //             Information("Message {0} succcessfully sent", postMessageResult.TimeStamp);
        //         }
        //         else
        //         {
        //             Error("Failed to send message: {0}", postMessageResult.Error);
        //         }
        //     }
        // }
    }

    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

#region Build

Task("Clean")
    .Does<BuildParameters>((parameters) =>
{
    Information("Cleaning directories..");

    CleanDirectories("./src/**/bin/" + parameters.Configuration);
    CleanDirectories("./src/**/obj");
    CleanDirectories("./src/GitVersionTfsTask/scripts/**");

    DeleteFiles("src/GitVersionTfsTask/*.vsix");
    DeleteFiles("src/GitVersionRubyGem/*.gem");

    CleanDirectories(parameters.Paths.Directories.ToClean);
});

// This build task can be run to just build
Task("DogfoodBuild")
    .IsDependentOn("Clean")
    .Does<BuildParameters>((parameters) =>
{
    Build(parameters.Configuration);
});

Task("Build")
    .IsDependentOn("Clean")
    .Does<BuildParameters>((parameters) =>
{
    Build(parameters.Configuration);
});

#endregion

#region Tests

Task("Test")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledUnitTests, "Unit tests were disabled.")
    .IsDependentOn("Build")
    .Does<BuildParameters>((parameters) =>
{
    var framework = "net461";

    // run using dotnet test
    var projects = GetFiles("./src/**/*.Tests.csproj");
    foreach(var project in projects)
    {
        var settings = new DotNetCoreTestSettings
        {
            Framework = framework,
            NoBuild = true,
            NoRestore = true,
            Configuration = parameters.Configuration
        };

        var coverletSettings = new CoverletSettings {
            CollectCoverage = true,
            CoverletOutputFormat = CoverletOutputFormat.opencover,
            CoverletOutputDirectory = parameters.Paths.Directories.TestCoverageOutput + "/",
            CoverletOutputName = $"{project.GetFilenameWithoutExtension()}.coverage.xml"
        };

        if (IsRunningOnUnix())
        {
            settings.Filter = "TestCategory!=NoMono";
        }

        // DotNetCoreTest(project.FullPath, settings, coverletSettings);
        DotNetCoreTest(project.FullPath, settings);
    }

    // run using NUnit
    var testAssemblies = GetFiles("./src/**/bin/" + parameters.Configuration + "/" + framework + "/*.Tests.dll");

    var nunitSettings = new NUnit3Settings
    {
        Results = new List<NUnit3Result> { new NUnit3Result { FileName = parameters.Paths.Files.TestCoverageOutputFilePath } }
    };

    if(IsRunningOnUnix()) {
        nunitSettings.Where = "cat!=NoMono";
        nunitSettings.Agents = 1;
    }

    NUnit3(testAssemblies, nunitSettings);
});

#endregion

#region Package

Task("Copy-Files")
    .IsDependentOn("Test")
    .Does<BuildParameters>((parameters) =>
{
    // .NET Core
    var netCoreDir = parameters.Paths.Directories.ArtifactsBinNetCore.Combine("tools");
    DotNetCorePublish("./src/GitVersionExe/GitVersionExe.csproj", new DotNetCorePublishSettings
    {
        Framework = parameters.NetCoreVersion,
        NoRestore = true,
        Configuration = parameters.Configuration,
        OutputDirectory = netCoreDir,
        MSBuildSettings = parameters.MSBuildSettings
    });

    // Copy license & Copy GitVersion.XML (since publish does not do this anymore)
    CopyFileToDirectory("./LICENSE", netCoreDir);
    CopyFileToDirectory($"./src/GitVersionExe/bin/{parameters.Configuration}/{parameters.NetCoreVersion}/GitVersion.xml", netCoreDir);

    // .NET 4.0
    DotNetCorePublish("./src/GitVersionExe/GitVersionExe.csproj", new DotNetCorePublishSettings
    {
        Framework = parameters.FullFxVersion,
        NoBuild = true,
        NoRestore = true,
        Configuration = parameters.Configuration,
        OutputDirectory = parameters.Paths.Directories.ArtifactsBinFullFx,
        MSBuildSettings = parameters.MSBuildSettings
    });

    var ilMergDir = parameters.Paths.Directories.ArtifactsBinFullFxILMerge;
    var portableDir = parameters.Paths.Directories.ArtifactsBinFullFxPortable.Combine("tools");
    var cmdlineDir = parameters.Paths.Directories.ArtifactsBinFullFxCmdline.Combine("tools");

    // Portable
    PublishILRepackedGitVersionExe(true, parameters.Paths.Directories.ArtifactsBinFullFx, ilMergDir, portableDir, parameters.Configuration, parameters.FullFxVersion);
    // Commandline
    PublishILRepackedGitVersionExe(false, parameters.Paths.Directories.ArtifactsBinFullFx, ilMergDir, cmdlineDir, parameters.Configuration, parameters.FullFxVersion);

    // Vsix
    var tfsPath = new DirectoryPath("./src/GitVersionTfsTask/GitVersionTask");
    EnsureDirectoryExists(tfsPath);
    CopyFileToDirectory(portableDir + "/" + "LibGit2Sharp.dll.config", tfsPath);
    CopyFileToDirectory(portableDir + "/" + "GitVersion.exe", tfsPath);
    CopyDirectory(portableDir.Combine("lib"), tfsPath.Combine("lib"));

    // Vsix dotnet core
    var tfsNetCorePath = new DirectoryPath("./src/GitVersionTfsTask/GitVersionNetCoreTask");
    EnsureDirectoryExists(tfsNetCorePath);
    CopyDirectory(netCoreDir, tfsNetCorePath.Combine("netcore"));

    // Ruby Gem
    var gemPath = new DirectoryPath("./src/GitVersionRubyGem/bin");
    EnsureDirectoryExists(gemPath);
    CopyFileToDirectory(portableDir + "/" + "LibGit2Sharp.dll.config", gemPath);
    CopyFileToDirectory(portableDir + "/" + "GitVersion.exe", gemPath);
    CopyDirectory(portableDir.Combine("lib"), gemPath.Combine("lib"));
});

Task("Pack-Tfs")
    .IsDependentOn("Copy-Files")
    .Does<BuildParameters>((parameters) =>
{
    var workDir = "./src/GitVersionTfsTask";

    // update version number
    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.mono.json"), "$version$", parameters.Version.SemVersion);
    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.netcore.json"), "$version$", parameters.Version.SemVersion);
    UpdateTaskVersion(new FilePath(workDir + "/GitVersionTask/task.json"), parameters.Version.GitVersion);
    UpdateTaskVersion(new FilePath(workDir + "/GitVersionNetCoreTask/task.json"), parameters.Version.GitVersion);

    // build and pack
    NpmSet("progress", "false");
    NpmInstall(new NpmInstallSettings { WorkingDirectory = workDir, LogLevel = NpmLogLevel.Silent });
    NpmRunScript(new NpmRunScriptSettings { WorkingDirectory = workDir, ScriptName = "build", LogLevel = NpmLogLevel.Silent  });

    TfxExtensionCreate(new TfxExtensionCreateSettings
    {
        ToolPath = workDir + "/node_modules/.bin/" + (parameters.IsRunningOnWindows ? "tfx.cmd" : "tfx"),
        WorkingDirectory = workDir,
        ManifestGlobs = new List<string>(){ "vss-extension.mono.json" },
        OutputPath = parameters.Paths.Directories.BuildArtifact
    });

    TfxExtensionCreate(new TfxExtensionCreateSettings
    {
        ToolPath = workDir + "/node_modules/.bin/" + (parameters.IsRunningOnWindows ? "tfx.cmd" : "tfx"),
        WorkingDirectory = workDir,
        ManifestGlobs = new List<string>(){ "vss-extension.netcore.json" },
        OutputPath = parameters.Paths.Directories.BuildArtifact
    });
});

Task("Pack-Gem")
    .IsDependentOn("Copy-Files")
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
    .IsDependentOn("Copy-Files")
    .Does<BuildParameters>((parameters) =>
{
    foreach(var package in parameters.Packages.Nuget)
    {
        if (FileExists(package.NuspecPath)) {
            var artifactPath = MakeAbsolute(parameters.PackagesBuildMap[package.Id]).FullPath;

            var nugetSettings = new NuGetPackSettings
            {
                Version = parameters.Version.SemVersion,
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
        NoBuild = true,
        NoRestore = true,
        MSBuildSettings = parameters.MSBuildSettings
    };

    // GitVersionCore, GitVersionTask, & global tool
    DotNetCorePack("./src/GitVersionCore", settings);
    DotNetCorePack("./src/GitVersionTask", settings);
    DotNetCorePack("./src/GitVersionExe/GitVersion.Tool.csproj", settings);
});

Task("Pack-Chocolatey")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,  "Pack-Chocolatey works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsMainBranch, "Pack-Chocolatey works only for main branch.")
    .IsDependentOn("Copy-Files")
    .Does<BuildParameters>((parameters) =>
{
    foreach(var package in parameters.Packages.Chocolatey)
    {
        if (FileExists(package.NuspecPath)) {
            var artifactPath = MakeAbsolute(parameters.PackagesBuildMap[package.Id]).FullPath;

            var files = GetFiles(artifactPath + "/**/*.*")
                        .Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") });
            var txtFiles = (GetFiles("./nuspec/*.txt") + GetFiles("./nuspec/*.ps1"))
                        .Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.GetFilename().ToString() });

            ChocolateyPack(package.NuspecPath, new ChocolateyPackSettings {
                Verbose = true,
                Version = parameters.Version.SemVersion,
                OutputDirectory = parameters.Paths.Directories.NugetRoot,
                Files = files.Concat(txtFiles).ToArray()
            });
        }
    }
});

Task("Zip-Files")
    .IsDependentOn("Copy-Files")
    .Does<BuildParameters>((parameters) =>
{
    // .NET 4.0
    var cmdlineDir = parameters.Paths.Directories.ArtifactsBinFullFxCmdline.Combine("tools");
    var fullFxFiles = GetFiles(cmdlineDir.FullPath + "/**/*");
    Zip(cmdlineDir, parameters.Paths.Files.ZipArtifactPathDesktop, fullFxFiles);

    // .NET Core
    var netCoreDir = parameters.Paths.Directories.ArtifactsBinNetCore.Combine("tools");
    var coreclrFiles = GetFiles(netCoreDir.FullPath + "/**/*");
    Zip(netCoreDir, parameters.Paths.Files.ZipArtifactPathCoreClr, coreclrFiles);
});

Task("Docker-Build")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Docker can be built only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease() || parameters.IsPreRelease(), "Docker-Build works only for releases.")
    .IsDependentOn("Copy-Files")
    .Does<BuildParameters>((parameters) =>
{
    if (parameters.IsRunningOnWindows)
    {
        DockerBuild("windows", "dotnetcore", parameters);
        DockerBuild("windows", "fullfx", parameters);
    }
    else if (parameters.IsRunningOnLinux)
    {
        DockerBuild("linux", "dotnetcore", parameters);
        DockerBuild("linux", "fullfx", parameters);
    }
});

Task("Pack")
    .IsDependentOn("Pack-Tfs")
    .IsDependentOn("Pack-Gem")
    .IsDependentOn("Pack-Nuget")
    .IsDependentOn("Pack-Chocolatey")
    .IsDependentOn("Zip-Files")
    .Does<BuildParameters>((parameters) =>
{
    Information("The build artifacts: \n");
    foreach(var artifact in parameters.Artifacts.All)
    {
        if (FileExists(artifact.ArtifactPath)) { Information("Artifact: {0}", artifact.ArtifactPath); }
    }

    foreach(var package in parameters.Packages.All)
    {
        if (FileExists(package.PackagePath)) { Information("Artifact: {0}", package.PackagePath); }
    }
})
    .ReportError(exception =>
{
    Error(exception.Dump());
});

#endregion

#region Publish

Task("Release-Notes")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,  "Release notes are generated only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAppVeyor, "Release notes are generated only on release agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease(),   "Release notes are generated only for stable releases.")
    .IsDependentOn("Clean")
    .Does<BuildParameters>((parameters) =>
{
    var outputFile = parameters.Paths.Files.ReleaseNotesOutputFilePath;
    var githubToken = parameters.Credentials.GitHub.Token;

    GetReleaseNotes(outputFile, ".", githubToken);
}).ReportError(exception =>
{
    Error(exception.Dump());
});

Task("Publish-Coverage")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,  "Publish-Coverage works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAppVeyor, "Publish-Coverage works only on AppVeyor.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-Coverage works only for releases.")
    .IsDependentOn("Test")
    .Does<BuildParameters>((parameters) =>
{
    var coverageFiles = GetFiles(parameters.Paths.Directories.TestCoverageOutput + "/*.coverage.xml");

    var token = parameters.Credentials.CodeCov.Token;
    if(string.IsNullOrEmpty(token)) {
        throw new InvalidOperationException("Could not resolve CodeCov token.");
    }

    foreach (var coverageFile in coverageFiles) {
        // Upload a coverage report using the CodecovSettings.
        Codecov(new CodecovSettings {
            Files = new [] { coverageFile.ToString() },
            Token = token
        });
    }
});

Task("Publish-AppVeyor")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows, "Publish-AppVeyor works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAppVeyor, "Publish-AppVeyor works only on AppVeyor.")
    .IsDependentOn("Pack")
    .IsDependentOn("Release-Notes")
    .Does<BuildParameters>((parameters) =>
{
    foreach(var artifact in parameters.Artifacts.All)
    {
        if (FileExists(artifact.ArtifactPath)) { AppVeyor.UploadArtifact(artifact.ArtifactPath); }
    }

    foreach(var package in parameters.Packages.All)
    {
        if (FileExists(package.PackagePath)) { AppVeyor.UploadArtifact(package.PackagePath); }
    }

    if (FileExists(parameters.Paths.Files.TestCoverageOutputFilePath)) {
        AppVeyor.UploadTestResults(parameters.Paths.Files.TestCoverageOutputFilePath, AppVeyorTestResultsType.NUnit3);
    }
})
.OnError(exception =>
{
    Information("Publish-AppVeyor Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish-AzurePipeline")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows, "Publish-AzurePipeline works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Publish-AzurePipeline works only on AzurePipeline.")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsPullRequest, "Publish-AzurePipeline works only for non-PR commits.")
    .IsDependentOn("Pack")
    .IsDependentOn("Release-Notes")
    .Does<BuildParameters>((parameters) =>
{
    foreach(var artifact in parameters.Artifacts.All)
    {
        if (FileExists(artifact.ArtifactPath)) { TFBuild.Commands.UploadArtifact(artifact.ContainerName, artifact.ArtifactPath, artifact.ArtifactName); }
    }
    foreach(var package in parameters.Packages.All)
    {
        if (FileExists(package.PackagePath)) { TFBuild.Commands.UploadArtifact("packages", package.PackagePath, package.PackageName); }
    }

    if (FileExists(parameters.Paths.Files.TestCoverageOutputFilePath)) {
        var data = new TFBuildPublishTestResultsData {
            TestResultsFiles = new[] { parameters.Paths.Files.TestCoverageOutputFilePath },
            TestRunner = TFTestRunnerType.NUnit
        };
        TFBuild.Commands.PublishTestResults(data);
    }
})
.OnError(exception =>
{
    Information("Publish-AzurePipeline Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish-Tfs")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledPublishTfs,   "Publish-Tfs was disabled.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,  "Publish-Tfs works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAppVeyor, "Publish-Tfs works only on AppVeyor.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease(),   "Publish-Tfs works only for releases.")
    .IsDependentOn("Pack-Tfs")
    .Does<BuildParameters>((parameters) =>
{
    var token = parameters.Credentials.Tfx.Token;
    if(string.IsNullOrEmpty(token)) {
        throw new InvalidOperationException("Could not resolve Tfx token.");
    }

    var workDir = "./src/GitVersionTfsTask";
    TfxExtensionPublish(parameters.Paths.Files.VsixOutputFilePath, new TfxExtensionPublishSettings
    {
        ToolPath = workDir + "/node_modules/.bin/" + (parameters.IsRunningOnWindows ? "tfx.cmd" : "tfx"),
        AuthType = TfxAuthType.Pat,
        Token = token
    });

    var netCoreWorkDir = "./src/GitVersionTfsTask.NetCore";
    TfxExtensionPublish(parameters.Paths.Files.VsixNetCoreOutputFilePath, new TfxExtensionPublishSettings
    {
        ToolPath = netCoreWorkDir + "/node_modules/.bin/" + (parameters.IsRunningOnWindows ? "tfx.cmd" : "tfx"),
        AuthType = TfxAuthType.Pat,
        Token = token
    });
})
.OnError(exception =>
{
    Information("Publish-Tfs Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish-Gem")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledPublishGem,   "Publish-Gem was disabled.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,  "Publish-Gem works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAppVeyor, "Publish-Gem works only on AppVeyor.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-Gem works only for releases.")
    .IsDependentOn("Pack-Gem")
    .Does<BuildParameters>((parameters) =>
{
    var apiKey = parameters.Credentials.RubyGem.ApiKey;
    if(string.IsNullOrEmpty(apiKey)) {
        throw new InvalidOperationException("Could not resolve Ruby Gem Api key.");
    }

    SetRubyGemPushApiKey(apiKey);

    var toolPath = FindToolInPath(IsRunningOnWindows() ? "gem.cmd" : "gem");
    GemPush(parameters.Paths.Files.GemOutputFilePath, new Cake.Gem.Push.GemPushSettings()
    {
        ToolPath = toolPath,
    });
})
.OnError(exception =>
{
    Information("Publish-Gem Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish-DockerHub")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledPublishDocker, "Publish-DockerHub was disabled.")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS,    "Publish-DockerHub works only on Windows and Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAppVeyor || (parameters.IsRunningOnTravis && !parameters.IsRunningOnMacOS), "Publish-DockerHub works only on AppVeyor or Travis.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-DockerHub works only for releases.")
    .IsDependentOn("Docker-Build")
    .Does<BuildParameters>((parameters) =>
{
    var username = parameters.Credentials.Docker.UserName;
    if (string.IsNullOrEmpty(username)) {
        throw new InvalidOperationException("Could not resolve Docker user name.");
    }

    var password = parameters.Credentials.Docker.Password;
    if (string.IsNullOrEmpty(password)) {
        throw new InvalidOperationException("Could not resolve Docker password.");
    }

    DockerLogin(parameters.Credentials.Docker.UserName, parameters.Credentials.Docker.Password);

    if (parameters.IsRunningOnWindows)
    {
        DockerPush("windows", "dotnetcore", parameters);
        DockerPush("windows", "fullfx", parameters);
    }
    else if (parameters.IsRunningOnLinux)
    {
        DockerPush("linux", "dotnetcore", parameters);
        DockerPush("linux", "fullfx", parameters);
    }

    DockerLogout();
})
.OnError(exception =>
{
    Information("Publish-DockerHub Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish-NuGet")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledPublishNuget, "Publish-NuGet was disabled.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,  "Publish-NuGet works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAppVeyor, "Publish-NuGet works only on AppVeyor.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-NuGet works only for releases.")
    .IsDependentOn("Pack-NuGet")
    .Does<BuildParameters>((parameters) =>
{
    var apiKey = parameters.Credentials.Nuget.ApiKey;
    if(string.IsNullOrEmpty(apiKey)) {
        throw new InvalidOperationException("Could not resolve NuGet API key.");
    }

    var apiUrl = parameters.Credentials.Nuget.ApiUrl;
    if(string.IsNullOrEmpty(apiUrl)) {
        throw new InvalidOperationException("Could not resolve NuGet API url.");
    }

    foreach(var package in parameters.Packages.Nuget)
    {
        if (FileExists(package.PackagePath))
        {
            // Push the package.
            NuGetPush(package.PackagePath, new NuGetPushSettings
            {
                ApiKey = apiKey,
                Source = apiUrl
            });
        }
    }
})
.OnError(exception =>
{
    Information("Publish-NuGet Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish-Chocolatey")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledPublishChocolatey, "Publish-Chocolatey was disabled.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,       "Publish-Chocolatey works only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAppVeyor,      "Publish-Chocolatey works only on AppVeyor.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-Chocolatey works only for releases.")
    .IsDependentOn("Pack-Chocolatey")
    .Does<BuildParameters>((parameters) =>
{
    var apiKey = parameters.Credentials.Chocolatey.ApiKey;
    if(string.IsNullOrEmpty(apiKey)) {
        throw new InvalidOperationException("Could not resolve Chocolatey API key.");
    }

    var apiUrl = parameters.Credentials.Chocolatey.ApiUrl;
    if(string.IsNullOrEmpty(apiUrl)) {
        throw new InvalidOperationException("Could not resolve Chocolatey API url.");
    }

    foreach(var package in parameters.Packages.Chocolatey)
    {
        if (FileExists(package.PackagePath))
        {
            // Push the package.
            ChocolateyPush(package.PackagePath, new ChocolateyPushSettings
            {
                ApiKey = apiKey,
                Source = apiUrl,
                Force = true
            });
        }
    }
})
.OnError(exception =>
{
    Information("Publish-Chocolatey Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish")
    .IsDependentOn("Publish-AppVeyor")
    .IsDependentOn("Publish-AzurePipeline")
    .IsDependentOn("Publish-Coverage")
    .IsDependentOn("Publish-NuGet")
    .IsDependentOn("Publish-Chocolatey")
    .IsDependentOn("Publish-Tfs")
    .IsDependentOn("Publish-Gem")
    .IsDependentOn("Publish-DockerHub")
    .Finally(() =>
{
    if (publishingError)
    {
        throw new Exception("An error occurred during the publishing of GitVersion. All publishing tasks have been attempted.");
    }
});

#endregion
Task("Default")
    .IsDependentOn("Publish");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
RunTarget(target);
