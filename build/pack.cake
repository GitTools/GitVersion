Task("Pack-Prepare")
    .IsDependentOn("Validate-Version")
    .Does<BuildParameters>((parameters) =>
{
    PackPrepareNative(Context, parameters);

    var sourceDir = parameters.Paths.Directories.Native.Combine(PlatformFamily.Windows.ToString()).Combine("win-x64");
    var sourceFiles = GetFiles(sourceDir + "/*.*");

    // Cmdline and Portable
    var cmdlineDir = parameters.Paths.Directories.ArtifactsBinCmdline.Combine("tools");
    var portableDir = parameters.Paths.Directories.ArtifactsBinPortable.Combine("tools");

    EnsureDirectoryExists(cmdlineDir);
    EnsureDirectoryExists(portableDir);

    CopyFiles(sourceFiles, cmdlineDir);

    sourceFiles += GetFiles("./build/nuspec/*.ps1") + GetFiles("./build/nuspec/*.txt");
    CopyFiles(sourceFiles, portableDir);
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
                // KeepTemporaryNuSpecFile = true,
                Version = parameters.Version.NugetVersion,
                NoPackageAnalysis = true,
                OutputDirectory = parameters.Paths.Directories.NugetRoot,
                Repository = new NuGetRepository {
                    Branch = parameters.Version.GitVersion.BranchName,
                    Commit = parameters.Version.GitVersion.Sha
                },
                Files = GetFiles(artifactPath + "/**/*.*")
                        .Select(file => new NuSpecContent { Source = file.FullPath, Target = file.FullPath.Replace(artifactPath, "") })
                        .Concat(
                            GetFiles("docs/**/package_icon.png")
                            .Select(file => new NuSpecContent { Source = file.FullPath, Target = "package_icon.png" })
                        )
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

    // GitVersion.MsBuild, global tool & core
    settings.ArgumentCustomization = arg => arg.Append("/p:PackAsTool=true");
    DotNetCorePack("./src/GitVersionExe/GitVersionExe.csproj", settings);

    settings.ArgumentCustomization = arg => arg.Append("/p:IsPackaging=true");
    DotNetCorePack("./src/GitVersion.MsBuild", settings);

    settings.ArgumentCustomization = null;
    DotNetCorePack("./src/GitVersion.Core", settings);
});

Task("Pack-Chocolatey")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows, "Pack-Chocolatey works only on Windows agents.")
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
    var platform = Context.Environment.Platform.Family;
    var runtimes = parameters.NativeRuntimes[platform];

    foreach (var runtime in runtimes)
    {
        var sourceDir = parameters.Paths.Directories.Native.Combine(platform.ToString().ToLower()).Combine(runtime);
        var targetDir = parameters.Paths.Directories.ArtifactsRoot.Combine("native");
        EnsureDirectoryExists(targetDir);

        var fileName = $"gitversion-{runtime}-{parameters.Version.SemVersion}.tar.gz".ToLower();
        var tarFile = targetDir.CombineWithFilePath(fileName);
        var filePaths = GetFiles($"{sourceDir}/**/*");
        GZipCompress(sourceDir, tarFile, filePaths);
    }
});

void PackPrepareNative(ICakeContext context, BuildParameters parameters)
{
    // publish single file for all native runtimes (self contained)
    var platform = Context.Environment.Platform.Family;
    var runtimes = parameters.NativeRuntimes[platform];

    foreach (var runtime in runtimes)
    {
        var outputPath = PackPrepareNative(context, parameters, runtime);

        // testing windows and macos artifacts, the linux is tested with docker
        if (platform != PlatformFamily.Linux)
        {
            context.Information("Validating native lib:");
            var nativeExe = outputPath.CombineWithFilePath(IsRunningOnWindows() ? "gitversion.exe" : "gitversion");
            ValidateOutput(nativeExe.FullPath, "/showvariable FullSemver", parameters.Version.GitVersion.FullSemVer);
        }
    }
}

DirectoryPath PackPrepareNative(ICakeContext context, BuildParameters parameters, string runtime)
{
    var platform = Context.Environment.Platform.Family;
    var outputPath = parameters.Paths.Directories.Native.Combine(platform.ToString().ToLower()).Combine(runtime);

    var settings = new DotNetCorePublishSettings
    {
        Framework = parameters.NetVersion50,
        Runtime = runtime,
        NoRestore = false,
        Configuration = parameters.Configuration,
        OutputDirectory = outputPath,
        MSBuildSettings = parameters.MSBuildSettings,
    };

    settings.ArgumentCustomization =
        arg => arg
        .Append("/p:PublishSingleFile=true");

    context.DotNetCorePublish("./src/GitVersionExe/GitVersionExe.csproj", settings);

    return outputPath;
}
