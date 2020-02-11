singleStageRun = !IsEnabled(Context, "ENABLED_MULTI_STAGE_BUILD", false);

Task("Artifacts-Prepare")
.WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-Prepare can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Artifacts-Prepare works only on AzurePipeline.")
    .IsDependentOnWhen("Pack-Nuget", singleStageRun)
    .Does<BuildParameters>((parameters) =>
{
});

Task("Artifacts-DotnetTool-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-DotnetTool-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Artifacts-DotnetTool-Test works only on AzurePipeline.")
    .IsDependentOn("Artifacts-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    var rootPrefix = parameters.DockerRootPrefix;
    var version = parameters.Version.NugetVersion;

    foreach(var dockerImage in parameters.Docker.Images)
    {
        var cmd = $"$result = dotnet tool install GitVersion.Tool --version {version} --tool-path {rootPrefix}/gitversion --add-source {rootPrefix}/nuget | out-null; ";
        cmd += "if($LASTEXITCODE -eq 0) { ";
        cmd += $"{rootPrefix}/gitversion/dotnet-gitversion {rootPrefix}/repo /showvariable FullSemver;";
        cmd += "} else { echo $result }";


        DockerTestArtifact(dockerImage, parameters, cmd);
    }
});

Task("Artifacts-MsBuild-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-MsBuild-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Artifacts-MsBuild-Test works only on AzurePipeline.")
    .IsDependentOn("Artifacts-Prepare")
    .Does<BuildParameters>((parameters) =>
{
    var rootPrefix = parameters.DockerRootPrefix;
    var version = parameters.Version.NugetVersion;

    foreach(var dockerImage in parameters.Docker.Images)
    {
        var (os, distro, targetframework) = dockerImage;
        var cmd = $"$result = dotnet build {rootPrefix}/repo/test --source {rootPrefix}/nuget --source https://api.nuget.org/v3/index.json -p:GitVersionTaskVersion={version} -p:TargetFramework={targetframework} *>&1; ";
        cmd += "if($LASTEXITCODE -eq 0) { ";
        cmd += $"dotnet {rootPrefix}/repo/test/build/corefx/{targetframework}/TestRepo.dll;";
        cmd += "} else { echo $result }";

        DockerTestArtifact(dockerImage, parameters, cmd);
    }
});

Task("Artifacts-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Artifacts-Test works only on AzurePipeline.")
    .IsDependentOn("Artifacts-DotnetTool-Test")
    .IsDependentOn("Artifacts-MsBuild-Test");
