
// Install addins.
#addin "nuget:?package=Cake.Gitter&version=0.9.0"
#addin "nuget:?package=Cake.Docker&version=0.9.6"
#addin "nuget:?package=Cake.Npm&version=0.15.0"
#addin "nuget:?package=Cake.Incubator&version=3.0.0"
#addin "nuget:?package=Cake.Json&version=3.0.0"
#addin "nuget:?package=Cake.Tfx&version=0.8.0"
#addin "nuget:?package=Cake.Gem&version=0.7.0"
#addin "nuget:?package=Newtonsoft.Json&version=9.0.1"

// Install tools.
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.9.0"
#tool "nuget:?package=GitReleaseNotes&version=0.7.1"
#tool "nuget:?package=ILRepack&version=2.0.16"

// Load other scripts.
#load "./build/parameters.cake"
#load "./build/utils.cake"

//////////////////////////////////////////////////////////////////////
// PARAMETERS
//////////////////////////////////////////////////////////////////////

BuildParameters parameters = BuildParameters.GetParameters(Context);
bool publishingError = false;
DotNetCoreMSBuildSettings msBuildSettings = null;

string dotnetVersion = "net40";
GitVersion gitVersion = null;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
    Build(parameters.Configuration, null);
    gitVersion = GetVersion(dotnetVersion);
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

    msBuildSettings = new DotNetCoreMSBuildSettings()
                            .WithProperty("Version", parameters.Version.SemVersion)
                            .WithProperty("AssemblyVersion", parameters.Version.Version)
                            .WithProperty("PackageVersion", parameters.Version.SemVersion)
                            .WithProperty("FileVersion", parameters.Version.Version);

    if(!parameters.IsRunningOnWindows)
    {
        var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

        // Use FrameworkPathOverride when not running on Windows.
        Information("Build will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
        msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
    }
});

Teardown(context =>
{
    Information("Starting Teardown...");

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
    .Does(() =>
{
    Information("Cleaning direcories..");

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
    .Does(() =>
{
    Build(parameters.Configuration, gitVersion);
});

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    Build(parameters.Configuration, gitVersion);
});

#endregion

#region Tests

Task("Test")
    .WithCriteria(() => parameters.EnabledUnitTests, "Unit tests were disabled.")
    .IsDependentOn("Build")
    .Does(() =>
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

        if (IsRunningOnUnix())
        {
            settings.Filter = "TestCategory!=NoMono";
        }

        DotNetCoreTest(project.FullPath, settings);
    }

    // run using NUnit
    var testAssemblies = GetFiles("./src/**/bin/" + parameters.Configuration + "/" + framework + "/*.Tests.dll");

    var nunitSettings = new NUnit3Settings
    {
        OutputFile = parameters.Paths.Files.TestCoverageOutputFilePath
    };

    if(IsRunningOnUnix()) {
        nunitSettings.Where = "cat != NoMono";
        nunitSettings.Agents = 1;
    }

    FixForMono(nunitSettings, "nunit3-console.exe");
    NUnit3(testAssemblies, nunitSettings);
});

#endregion

#region Package

Task("Copy-Files")
    .IsDependentOn("Test")
    .Does(() =>
{
    var netCoreDir = parameters.Paths.Directories.ArtifactsBinNetCore.Combine("tools");
    // .NET Core
    DotNetCorePublish("./src/GitVersionExe/GitVersionExe.csproj", new DotNetCorePublishSettings
    {
        Framework = "netcoreapp2.0",
        NoRestore = true,
        Configuration = parameters.Configuration,
        OutputDirectory = netCoreDir,
        MSBuildSettings = msBuildSettings
    });

    // Copy license & Copy GitVersion.XML (since publish does not do this anymore)
    CopyFileToDirectory("./LICENSE", netCoreDir);
    CopyFileToDirectory("./src/GitVersionExe/bin/" + parameters.Configuration + "/netcoreapp2.0/GitVersion.xml", netCoreDir);

    // .NET 4.0
    DotNetCorePublish("./src/GitVersionExe/GitVersionExe.csproj", new DotNetCorePublishSettings
    {
        Framework = dotnetVersion,
        NoBuild = true,
        NoRestore = true,
        Configuration = parameters.Configuration,
        OutputDirectory = parameters.Paths.Directories.ArtifactsBinFullFx,
        MSBuildSettings = msBuildSettings
    });

    var ilMergDir = parameters.Paths.Directories.ArtifactsBinFullFxILMerge;
    var portableDir = parameters.Paths.Directories.ArtifactsBinFullFxPortable.Combine("tools");
    var cmdlineDir = parameters.Paths.Directories.ArtifactsBinFullFxCmdline.Combine("tools");

    // Portable
    PublishILRepackedGitVersionExe(true, parameters.Paths.Directories.ArtifactsBinFullFx, ilMergDir, portableDir, parameters.Configuration, dotnetVersion);
    // Commandline
    PublishILRepackedGitVersionExe(false, parameters.Paths.Directories.ArtifactsBinFullFx, ilMergDir, cmdlineDir, parameters.Configuration, dotnetVersion);

    // Vsix
    var tfsPath = new DirectoryPath("./src/GitVersionTfsTask/GitVersionTask");
    EnsureDirectoryExists(tfsPath);
    CopyFileToDirectory(portableDir + "/" + "LibGit2Sharp.dll.config", tfsPath);
    CopyFileToDirectory(portableDir + "/" + "GitVersion.exe", tfsPath);
    CopyDirectory(portableDir.Combine("lib"), tfsPath.Combine("lib"));

    // Ruby Gem
    var gemPath = new DirectoryPath("./src/GitVersionRubyGem/bin");
    EnsureDirectoryExists(gemPath);
    CopyFileToDirectory(portableDir + "/" + "LibGit2Sharp.dll.config", gemPath);
    CopyFileToDirectory(portableDir + "/" + "GitVersion.exe", gemPath);
    CopyDirectory(portableDir.Combine("lib"), gemPath.Combine("lib"));
});

Task("Pack-Tfs")
    .IsDependentOn("Copy-Files")
    .Does(() =>
{
    var workDir = "./src/GitVersionTfsTask";

    // update version number
    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.json"), "$version$", parameters.Version.SemVersion);

    var taskJsonFile = new FilePath(workDir + "/GitVersionTask/task.json");
    var taskJson = ParseJsonFromFile(taskJsonFile);
    taskJson["version"]["Major"] = gitVersion.Major.ToString();
    taskJson["version"]["Minor"] = gitVersion.Minor.ToString();
    taskJson["version"]["Patch"] = gitVersion.Patch.ToString();
    SerializeJsonToPrettyFile(taskJsonFile, taskJson);

    // build and pack
    NpmSet("progress", "false");
    NpmInstall(new NpmInstallSettings { WorkingDirectory = workDir, LogLevel = NpmLogLevel.Silent });
    NpmRunScript(new NpmRunScriptSettings { WorkingDirectory = workDir, ScriptName = "build", LogLevel = NpmLogLevel.Silent  });

    TfxExtensionCreate(new TfxExtensionCreateSettings
    {
        ToolPath = workDir + "/node_modules/.bin/" + (parameters.IsRunningOnWindows ? "tfx.cmd" : "tfx"),
        WorkingDirectory = workDir,
        ManifestGlobs = new List<string>(){ "vss-extension.json" },
        OutputPath = parameters.Paths.Directories.BuildArtifact
    });
});

Task("Pack-Gem")
    .IsDependentOn("Copy-Files")
    .Does(() =>
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
    .Does(() =>
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

            FixForMono(nugetSettings, "nuget.exe");
            NuGetPack(package.NuspecPath, nugetSettings);
        }
    }

    var settings = new DotNetCorePackSettings
    {
        Configuration = parameters.Configuration,
        OutputDirectory = parameters.Paths.Directories.NugetRoot,
        NoBuild = true,
        NoRestore = true,
        MSBuildSettings = msBuildSettings
    };

    // GitVersionCore & GitVersionTask
    DotNetCorePack("./src/GitVersionCore", settings);
    DotNetCorePack("./src/GitVersionTask", settings);
});

Task("Pack-Chocolatey")
    .WithCriteria(() => parameters.IsRunningOnWindows,  "Pack-Chocolatey works only on Windows agents.")
    .IsDependentOn("Copy-Files")
    .Does(() =>
{
    foreach(var package in parameters.Packages.Chocolatey)
    {
        if (FileExists(package.NuspecPath)) {
            var artifactPath = MakeAbsolute(parameters.PackagesBuildMap[package.Id]).FullPath;

            var files = GetFiles(artifactPath + "/**/*.*")
                        .Select(file => new ChocolateyNuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") });
            var txtFiles = GetFiles("./nuspec/*.txt")
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
    .Does(() =>
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
    .WithCriteria(() => !parameters.IsRunningOnMacOS, "Docker can be built only on Windows or Linux agents.")
    .WithCriteria(() => parameters.IsStableRelease() || parameters.IsPreRelease(), "Docker-Build works only for releases.")
    .IsDependentOn("Copy-Files")
    .Does(() =>
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
    .Does(() =>
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
    .WithCriteria(() => parameters.IsRunningOnWindows,  "Release notes are generated only on Windows agents.")
    .WithCriteria(() => parameters.IsRunningOnAppVeyor, "Release notes are generated only on release agents.")
    .WithCriteria(() => parameters.IsStableRelease(),   "Release notes are generated only for stable releases.")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var outputFile = parameters.Paths.Files.ReleaseNotesOutputFilePath;
    var githubToken = parameters.Credentials.GitHub.Token;

    GetReleaseNotes(outputFile, ".", githubToken);
}).ReportError(exception =>
{
    Error(exception.Dump());
});

Task("Publish-AppVeyor")
    .WithCriteria(() => parameters.IsRunningOnWindows, "Publish-AppVeyor works only on Windows agents.")
    .WithCriteria(() => parameters.IsRunningOnAppVeyor, "Publish-AppVeyor works only on AppVeyor.")
    .IsDependentOn("Pack")
    .IsDependentOn("Release-Notes")
    .Does(() =>
{
    foreach(var artifact in parameters.Artifacts.All)
    {
        if (FileExists(artifact.ArtifactPath)) { AppVeyor.UploadArtifact(artifact.ArtifactPath); }
    }

    foreach(var package in parameters.Packages.All)
    {
        if (FileExists(package.PackagePath)) { AppVeyor.UploadArtifact(package.PackagePath); }
    }
})
.OnError(exception =>
{
    Information("Publish-AppVeyor Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish-AzurePipeline")
    .WithCriteria(() => parameters.IsRunningOnWindows, "Publish-AzurePipeline works only on Windows agents.")
    .WithCriteria(() => parameters.IsRunningOnAzurePipeline, "Publish-AzurePipeline works only on AzurePipeline.")
    .IsDependentOn("Pack")
    .IsDependentOn("Release-Notes")
    .Does(() =>
{
    foreach(var artifact in parameters.Artifacts.All)
    {
        if (FileExists(artifact.ArtifactPath)) { TFBuild.Commands.UploadArtifact(artifact.ContainerName, artifact.ArtifactPath, artifact.ArtifactName); }
    }
    foreach(var package in parameters.Packages.All)
    {
        if (FileExists(package.PackagePath)) { TFBuild.Commands.UploadArtifact("packages", package.PackagePath, package.PackageName); }
    }
})
.OnError(exception =>
{
    Information("Publish-AzurePipeline Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish-Tfs")
    .WithCriteria(() => parameters.EnabledPublishTfs,   "Publish-Tfs was disabled.")
    .WithCriteria(() => parameters.IsRunningOnWindows,  "Publish-Tfs works only on Windows agents.")
    .WithCriteria(() => parameters.IsRunningOnAppVeyor, "Publish-Tfs works only on AppVeyor.")
    .WithCriteria(() => parameters.IsStableRelease(), "Publish-Tfs works only for releases.")
    .IsDependentOn("Pack-Tfs")
    .Does(() =>
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
})
.OnError(exception =>
{
    Information("Publish-Tfs Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});

Task("Publish-Gem")
    .WithCriteria(() => parameters.EnabledPublishGem,   "Publish-Gem was disabled.")
    .WithCriteria(() => parameters.IsRunningOnWindows,  "Publish-Gem works only on Windows agents.")
    .WithCriteria(() => parameters.IsRunningOnAppVeyor, "Publish-Gem works only on AppVeyor.")
    .WithCriteria(() => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-Gem works only for releases.")
    .IsDependentOn("Pack-Gem")
    .Does(() =>
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
    .WithCriteria(() => parameters.EnabledPublishDocker, "Publish-DockerHub was disabled.")
    .WithCriteria(() => !parameters.IsRunningOnMacOS,    "Publish-DockerHub works only on Windows and Linux agents.")
    .WithCriteria(() => parameters.IsRunningOnAppVeyor || (parameters.IsRunningOnTravis && !parameters.IsRunningOnMacOS), "Publish-DockerHub works only on AppVeyor or Travis.")
    .WithCriteria(() => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-DockerHub works only for releases.")
    .IsDependentOn("Docker-Build")
    .Does(() =>
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
    .WithCriteria(() => parameters.EnabledPublishNuget, "Publish-NuGet was disabled.")
    .WithCriteria(() => parameters.IsRunningOnWindows,  "Publish-NuGet works only on Windows agents.")
    .WithCriteria(() => parameters.IsRunningOnAppVeyor, "Publish-NuGet works only on AppVeyor.")
    .WithCriteria(() => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-NuGet works only for releases.")
    .IsDependentOn("Pack-NuGet")
    .Does(() =>
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
    .WithCriteria(() => parameters.EnabledPublishChocolatey, "Publish-Chocolatey was disabled.")
    .WithCriteria(() => parameters.IsRunningOnWindows,       "Publish-Chocolatey works only on Windows agents.")
    .WithCriteria(() => parameters.IsRunningOnAppVeyor,      "Publish-Chocolatey works only on AppVeyor.")
    .WithCriteria(() => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-Chocolatey works only for releases.")
    .IsDependentOn("Pack-Chocolatey")
    .Does(() =>
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

RunTarget(parameters.Target);
