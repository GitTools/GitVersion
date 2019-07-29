singleStageRun = !IsEnabled(Context, "ENABLED_MULTI_STAGE_BUILD", false);

Task("Artifacts-DotnetTool-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-DotnetTool-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Artifacts-DotnetTool-Test works only on AzurePipeline.")
    .IsDependentOnWhen("Pack-Nuget", singleStageRun)
    .Does<BuildParameters>((parameters) =>
{
    var currentDir = MakeAbsolute(Directory("."));
    var rootDir = parameters.IsDockerForWindows ? "c:/" : "/";
    var settings = new DockerContainerRunSettings
    {
        Rm = true,
        Volume = new[]
        {
            $"{currentDir}:{rootDir}repo",
            $"{currentDir}/artifacts/v{parameters.Version.SemVersion}/nuget:{rootDir}nuget"
        }
    };

    foreach(var dockerImage in parameters.Docker.Images)
    {
        var (os, distro, targetframework) = dockerImage;
        var tag = $"gittools/build-images:{distro}-sdk-{targetframework.Replace("netcoreapp", "")}";
        Information("Docker tag: {0}", tag);

        var version = parameters.Version.NugetVersion;

        var cmd = $"dotnet tool install GitVersion.Tool --version {version} --tool-path {rootDir}gitversion --add-source {rootDir}nuget | out-null; ";
        cmd += $"{rootDir}gitversion/dotnet-gitversion {rootDir}repo /showvariable FullSemver;";
        Information("Docker cmd: {0}", cmd);

        DockerTestRun(settings, parameters, tag, "pwsh", cmd);
    }
});

Task("Artifacts-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Artifacts-Test can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Artifacts-Test works only on AzurePipeline.")
    .IsDependentOn("Artifacts-DotnetTool-Test");
