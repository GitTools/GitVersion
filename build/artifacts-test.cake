singleStageRun = !IsEnabled(Context, "ENABLED_MULTI_STAGE_BUILD", false);

Task("Artifacts-Prepare")
.WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-Prepare can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI, "Artifacts-Prepare works only on Releasing CI.")
    .IsDependentOnWhen("Pack-Nuget", singleStageRun)
    .Does<BuildParameters>((parameters) =>
{
    foreach(var dockerImage in parameters.Docker.Images)
    {
        DockerPullImage(dockerImage, parameters);
    }
});

Task("Artifacts-DotnetTool-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-DotnetTool-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,     "Artifacts-DotnetTool-Test works only on Releasing CI.")
    .IsDependentOn("Artifacts-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    var rootPrefix = parameters.DockerRootPrefix;
    var version = parameters.Version.NugetVersion;

    foreach(var dockerImage in parameters.Docker.Images)
    {
        var cmd = $"-file {rootPrefix}/scripts/Test-DotnetGlobalTool.ps1 -version {version} -repoPath {rootPrefix}/repo -nugetPath {rootPrefix}/nuget";

        DockerTestArtifact(dockerImage, parameters, cmd);
    }
});

Task("Artifacts-Commandline-Test")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows, "Artifacts-Commandline-Test can be tested only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,      "Artifacts-Commandline-Test works only on Releasing CI.")
    .IsDependentOnWhen("Pack-Nuget", singleStageRun)
    .Does<BuildParameters>((parameters) =>
{
    NuGetInstall("GitVersion.Commandline", new NuGetInstallSettings {
        Source = new string[] { MakeAbsolute(parameters.Paths.Directories.NugetRoot).FullPath },
        ExcludeVersion  = true,
        Prerelease = true,
        OutputDirectory = parameters.Paths.Directories.ArtifactsRoot
    });

    var settings = new GitVersionSettings
    {
        OutputType = GitVersionOutput.Json,
        ToolPath = parameters.Paths.Directories.ArtifactsRoot.Combine("GitVersion.Commandline/tools").CombineWithFilePath("gitversion.exe").FullPath
    };
    var gitVersion = GitVersion(settings);

    Assert.Equal(parameters.Version.GitVersion.FullSemVer, gitVersion.FullSemVer);
});

Task("Artifacts-Portable-Test")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows, "Artifacts-Portable-Test can be tested only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,      "Artifacts-Portable-Test works only on Releasing CI.")
    .IsDependentOnWhen("Pack-Chocolatey", singleStageRun)
    .Does<BuildParameters>((parameters) =>
{
    if (parameters.IsMainBranch && !parameters.IsPullRequest) {
        NuGetInstall("GitVersion.Portable", new NuGetInstallSettings {
            Source = new string[] { MakeAbsolute(parameters.Paths.Directories.NugetRoot).FullPath },
            ExcludeVersion  = true,
            Prerelease = true,
            OutputDirectory = parameters.Paths.Directories.ArtifactsRoot
        });

        var settings = new GitVersionSettings
        {
            OutputType = GitVersionOutput.Json,
            ToolPath = parameters.Paths.Directories.ArtifactsRoot.Combine("GitVersion.Portable/tools").CombineWithFilePath("gitversion.exe").FullPath
        };
        var gitVersion = GitVersion(settings);

        Assert.Equal(parameters.Version.GitVersion.FullSemVer, gitVersion.FullSemVer);
    }
});

Task("Artifacts-Native-Test")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnLinux, "Artifacts-Native-Test can be tested only on Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,    "Artifacts-Native-Test works only on Releasing CI.")
    .IsDependentOn("Artifacts-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    var rootPrefix = parameters.DockerRootPrefix;
    var version = parameters.Version.NugetVersion;

    foreach(var dockerImage in parameters.Docker.Images)
    {
        var (os, distro, targetframework) = dockerImage;

        PackPrepareNative(Context, parameters, distro);

        var cmd = $"-file {rootPrefix}/scripts/Test-Native.ps1 -repoPath {rootPrefix}/repo -runtime {distro}";

        DockerTestArtifact(dockerImage, parameters, cmd);
    }
});

Task("Artifacts-MsBuildCore-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-MsBuildCore-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,     "Artifacts-MsBuildCore-Test works only on Releasing CI.")
    .IsDependentOn("Artifacts-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    var rootPrefix = parameters.DockerRootPrefix;
    var version = parameters.Version.NugetVersion;

    foreach(var dockerImage in parameters.Docker.Images)
    {
        var (os, distro, targetframework) = dockerImage;

        // TODO investigate
        if (distro == "alpine.3.10-x64" && targetframework == "netcoreapp3.1") {
            Information("Skipping this combination, works locally, not in CI for some reason");
            continue;
        }

        var cmd = $"-file {rootPrefix}/scripts/Test-MsBuildCore.ps1 -version {version} -repoPath {rootPrefix}/repo/test/core -nugetPath {rootPrefix}/nuget -targetframework {targetframework}";

        DockerTestArtifact(dockerImage, parameters, cmd);
    }
});

Task("Artifacts-MsBuildFull-Test")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows, "Artifacts-MsBuildFull-Test can be tested only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,      "Artifacts-MsBuildFull-Test works only on Releasing CI.")
    .IsDependentOnWhen("Pack-Nuget", singleStageRun)
    .Does<BuildParameters>((parameters) =>
{
    var version = parameters.Version.NugetVersion;

    var nugetSource = MakeAbsolute(parameters.Paths.Directories.NugetRoot).FullPath;

    Information("\nTesting msbuild task with dotnet build (for .net core)\n");
    var frameworks = new[] { parameters.CoreFxVersion21, parameters.CoreFxVersion31 };
    foreach(var framework in frameworks)
    {
        var dotnetCoreMsBuildSettings = new DotNetCoreMSBuildSettings();
        dotnetCoreMsBuildSettings.WithProperty("TargetFramework", framework);
        dotnetCoreMsBuildSettings.WithProperty("GitVersionTaskVersion", version);

        var projPath = MakeAbsolute(new DirectoryPath("./test/core"));

        DotNetCoreBuild(projPath.FullPath, new DotNetCoreBuildSettings
        {
            Verbosity = DotNetCoreVerbosity.Minimal,
            Configuration = parameters.Configuration,
            MSBuildSettings = dotnetCoreMsBuildSettings,
            ArgumentCustomization = args => args.Append($"--source {nugetSource}")
        });

        var netcoreExe = new DirectoryPath("./test/core/build").Combine(framework).CombineWithFilePath("app.dll");
        ValidateOutput("dotnet", netcoreExe.FullPath, parameters.Version.GitVersion.FullSemVer);
    }

    Information("\nTesting msbuild task with msbuild (for full framework)\n");

    var msBuildSettings = new MSBuildSettings
    {
        Verbosity = Verbosity.Minimal,
        Restore = true
    };

    msBuildSettings.WithProperty("GitVersionTaskVersion", version);
    msBuildSettings.WithProperty("RestoreSource", nugetSource);

    MSBuild("./test/full", msBuildSettings);

    var fullExe = new DirectoryPath("./test/full/build").CombineWithFilePath("app.exe");
    ValidateOutput(fullExe.FullPath, null, parameters.Version.GitVersion.FullSemVer);
});

Task("Artifacts-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,     "Artifacts-Test works only on Releasing CI.")
    .IsDependentOn("Artifacts-Native-Test")
    .IsDependentOn("Artifacts-DotnetTool-Test")
    .IsDependentOn("Artifacts-MsBuildCore-Test");
