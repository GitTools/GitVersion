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
        var cmd = $"-file {rootPrefix}/scripts/Test-DotnetGlobalTool.ps1 -version {version} -repoPath {rootPrefix}/repo -nugetPath {rootPrefix}/nuget -toolPath {rootPrefix}/gitversion";

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

Task("Artifacts-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,     "Artifacts-Test works only on Releasing CI.")
    .IsDependentOn("Artifacts-DotnetTool-Test")
    .IsDependentOn("Artifacts-MsBuildCore-Test");
