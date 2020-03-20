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

        var cmd = $"-file {rootPrefix}/scripts/Test-MsBuildCore.ps1 -version {version} -repoPath {rootPrefix}/repo/test/core -nugetPath {rootPrefix}/nuget -targetframework {targetframework}";

        DockerTestArtifact(dockerImage, parameters, cmd);
    }
});

Task("Artifacts-MsBuildFull-Library-Test")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows, "Artifacts-MsBuildFull-Library-Test can be tested only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,      "Artifacts-MsBuildFull-Library-Test works only on Releasing CI.")
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

Task("Artifacts-MsBuildFull-SystemWeb-Test")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows, "Artifacts-MsBuildFull-SystemWeb-Test can be tested only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,      "Artifacts-MsBuildFull-SystemWeb-Test works only on Releasing CI.")
    .IsDependentOnWhen("Pack-Nuget", singleStageRun)
    .Does<BuildParameters>((parameters) =>
{
    var version = parameters.Version.NugetVersion;

    var nugetSource = MakeAbsolute(parameters.Paths.Directories.NugetRoot).FullPath;

    Information("\nTesting msbuild task with msbuild (for full framework - System.Web publishing)\n");

    var msBuildSettings = new MSBuildSettings
    {
        Verbosity = Verbosity.Minimal,
        Restore = true
    };

    msBuildSettings.WithProperty("GitVersionTaskVersion", version);
    msBuildSettings.WithProperty("RestoreSources", nugetSource);
    msBuildSettings.WithProperty("DeployOnBuild", "True");
    msBuildSettings.WithProperty("PublishProfile", "TestPublish");

    MSBuild("./test/systemweb", msBuildSettings);

    var mergedPrecompiledDll = new DirectoryPath("./test/systemweb/build/TestPublish/bin").CombineWithFilePath("TestPublishAssembly.dll");
    ValidateAssemblyVersion(mergedPrecompiledDll.FullPath, parameters.Version.GitVersion.InformationalVersion);

    var webProjectDll = new DirectoryPath("./test/systemweb/build/TestPublish/bin").CombineWithFilePath("test.dll");
    ValidateAssemblyVersion(webProjectDll.FullPath, parameters.Version.GitVersion.InformationalVersion);
});

Task("Artifacts-MsBuildFull-Test")
    .IsDependentOn("Artifacts-MsBuildFull-SystemWeb-Test")
    .IsDependentOn("Artifacts-MsBuildFull-Library-Test");

Task("Artifacts-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,     "Artifacts-Test works only on Releasing CI.")
    .IsDependentOn("Artifacts-DotnetTool-Test")
    .IsDependentOn("Artifacts-MsBuildCore-Test");
