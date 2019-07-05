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
    var framework = parameters.FullFxVersion;

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

        DotNetCoreTest(project.FullPath, settings, coverletSettings);
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
    var coreFxDir = parameters.Paths.Directories.ArtifactsBinCoreFx.Combine("tools");
    DotNetCorePublish("./src/GitVersionExe/GitVersionExe.csproj", new DotNetCorePublishSettings
    {
        Framework = parameters.CoreFxVersion,
        NoRestore = true,
        Configuration = parameters.Configuration,
        OutputDirectory = coreFxDir,
        MSBuildSettings = parameters.MSBuildSettings
    });

    // Copy license & Copy GitVersion.XML (since publish does not do this anymore)
    CopyFileToDirectory("./LICENSE", coreFxDir);
    CopyFileToDirectory($"./src/GitVersionExe/bin/{parameters.Configuration}/{parameters.CoreFxVersion}/GitVersion.xml", coreFxDir);

    // .NET Framework
    DotNetCorePublish("./src/GitVersionExe/GitVersionExe.csproj", new DotNetCorePublishSettings
    {
        Framework = parameters.FullFxVersion,
        NoBuild = true,
        NoRestore = true,
        Configuration = parameters.Configuration,
        OutputDirectory = parameters.Paths.Directories.ArtifactsBinFullFx,
        MSBuildSettings = parameters.MSBuildSettings
    });

    DotNetCorePublish("./src/GitVersionTask/GitVersionTask.csproj", new DotNetCorePublishSettings
    {
        Framework = parameters.FullFxVersion,
        NoBuild = true,
        NoRestore = true,
        Configuration = parameters.Configuration,
        MSBuildSettings = parameters.MSBuildSettings
    });

    // .NET Core
    DotNetCorePublish("./src/GitVersionTask/GitVersionTask.csproj", new DotNetCorePublishSettings
    {
        Framework = parameters.CoreFxVersion,
        NoBuild = true,
        NoRestore = true,
        Configuration = parameters.Configuration,
        MSBuildSettings = parameters.MSBuildSettings
    });
    var ilMergeDir = parameters.Paths.Directories.ArtifactsBinFullFxILMerge;
    var portableDir = parameters.Paths.Directories.ArtifactsBinFullFxPortable.Combine("tools");
    var cmdlineDir = parameters.Paths.Directories.ArtifactsBinFullFxCmdline.Combine("tools");

    // Portable
    PublishILRepackedGitVersionExe(true, parameters.Paths.Directories.ArtifactsBinFullFx, ilMergeDir, portableDir, parameters.Configuration, parameters.FullFxVersion);
    // Commandline
    PublishILRepackedGitVersionExe(false, parameters.Paths.Directories.ArtifactsBinFullFx, ilMergeDir, cmdlineDir, parameters.Configuration, parameters.FullFxVersion);

    // Vsix
    var vsixPath = new DirectoryPath("./src/GitVersionTfsTask/GitVersionTask");

    var vsixPathFull = vsixPath.Combine("full");
    EnsureDirectoryExists(vsixPathFull);
    CopyFileToDirectory(portableDir + "/" + "LibGit2Sharp.dll.config", vsixPathFull);
    CopyFileToDirectory(portableDir + "/" + "GitVersion.exe", vsixPathFull);
    CopyDirectory(portableDir.Combine("lib"), vsixPathFull.Combine("lib"));

    // Vsix dotnet core
    var vsixPathCore = vsixPath.Combine("core");
    EnsureDirectoryExists(vsixPathCore);
    CopyDirectory(coreFxDir, vsixPathCore);

    // Ruby Gem
    var gemPath = new DirectoryPath("./src/GitVersionRubyGem/bin");
    EnsureDirectoryExists(gemPath);
    CopyFileToDirectory(portableDir + "/" + "LibGit2Sharp.dll.config", gemPath);
    CopyFileToDirectory(portableDir + "/" + "GitVersion.exe", gemPath);
    CopyDirectory(portableDir.Combine("lib"), gemPath.Combine("lib"));
});

Task("Pack-Vsix")
    .IsDependentOn("Copy-Files")
    .Does<BuildParameters>((parameters) =>
{
    var workDir = "./src/GitVersionTfsTask";
    var idSuffix    = parameters.IsStableRelease() ? "" : "-preview";
    var titleSuffix = parameters.IsStableRelease() ? "" : " (Preview)";
    var visibility  = parameters.IsStableRelease() ? "Public" : "Preview";
    var taskId      = parameters.IsStableRelease() ? "e5983830-3f75-11e5-82ed-81492570a08e" : "25b46667-d5a9-4665-97f7-e23de366ecdf";

    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.json"), "$idSuffix$", idSuffix);
    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.json"), "$titleSuffix$", titleSuffix);
    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.json"), "$visibility$", visibility);

    // update version number
    ReplaceTextInFile(new FilePath(workDir + "/vss-extension.json"), "$version$", parameters.Version.VsixVersion);
    UpdateTaskVersion(new FilePath(workDir + "/GitVersionTask/task.json"), taskId, parameters.Version.GitVersion);

    // build and pack
    NpmSet(new NpmSetSettings             { WorkingDirectory = workDir, LogLevel = NpmLogLevel.Silent, Key = "progress", Value = "false" });
    NpmInstall(new NpmInstallSettings     { WorkingDirectory = workDir, LogLevel = NpmLogLevel.Silent });
    NpmRunScript(new NpmRunScriptSettings { WorkingDirectory = workDir, LogLevel = NpmLogLevel.Silent, ScriptName = "build" });

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
    // .NET Framework
    var cmdlineDir = parameters.Paths.Directories.ArtifactsBinFullFxCmdline.Combine("tools");
    var fullFxFiles = GetFiles(cmdlineDir.FullPath + "/**/*");
    Zip(cmdlineDir, parameters.Paths.Files.ZipArtifactPathDesktop, fullFxFiles);

    // .NET Core
    var coreFxDir = parameters.Paths.Directories.ArtifactsBinCoreFx.Combine("tools");
    var coreclrFiles = GetFiles(coreFxDir.FullPath + "/**/*");
    Zip(coreFxDir, parameters.Paths.Files.ZipArtifactPathCoreClr, coreclrFiles);
});
#endregion
