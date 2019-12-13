#region Build

Task("Clean")
    .Does<BuildParameters>((parameters) =>
{
    Information("Cleaning directories..");

    CleanDirectories("./src/**/bin/" + parameters.Configuration);
    CleanDirectories("./src/**/obj");
    DeleteFiles("src/GitVersionRubyGem/*.gem");

    CleanDirectories(parameters.Paths.Directories.ToClean);
});

Task("Build")
    .IsDependentOn("Clean")
    .Does<BuildParameters>((parameters) =>
{
    Build(parameters);
    PublishGitVersionToArtifacts(parameters);

    RunGitVersionOnCI(parameters);
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
            Framework = parameters.CoreFxVersion31,
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

    var frameworks = new[] { parameters.CoreFxVersion21, parameters.FullFxVersion472 };

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

Task("Pack-Gem")
    .IsDependentOn("Pack-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    var workDir = "./src/GitVersionRubyGem";

    var gemspecFile = new FilePath(workDir + "/gitversion.gemspec");
    // update version number
    ReplaceTextInFile(gemspecFile, "$version$", parameters.Version.GemVersion);

    var toolPath = Context.FindToolInPath(IsRunningOnWindows() ? "gem.cmd" : "gem");
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
