singleStageRun = !IsEnabled(Context, "ENABLED_MULTI_STAGE_BUILD", false);

Task("Docker-Build")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Docker can be built only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Docker-Build works only on AzurePipeline.")
    .IsDependentOnWhen("Copy-Files", singleStageRun)
    .Does<BuildParameters>((parameters) =>
{
    foreach(var dockerImage in parameters.Docker.Images)
    {
        DockerBuild(dockerImage, parameters);
    }
});

Task("Docker-Test")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS, "Docker can be tested only on Windows or Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Docker-Test works only on AzurePipeline.")
    .IsDependentOn("Docker-Build")
    .Does<BuildParameters>((parameters) =>
{
    var currentDir = MakeAbsolute(Directory("."));
    var containerDir = parameters.IsDockerForWindows ? "c:/repo" : "/repo";
    var settings = new DockerContainerRunSettings
    {
        Rm = true,
        Volume = new[] { $"{currentDir}:{containerDir}" }
    };

    foreach(var dockerImage in parameters.Docker.Images)
    {
        var tags = GetDockerTags(dockerImage, parameters);
        foreach (var tag in tags)
        {
            DockerTestRun(settings, parameters, tag, containerDir);
        }
    }
});

Task("Publish-DockerHub")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledPublishDocker,     "Publish-DockerHub was disabled.")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsRunningOnMacOS,        "Publish-DockerHub works only on Windows and Linux agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Publish-DockerHub works only on AzurePipeline.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease() || parameters.IsPreRelease(), "Publish-DockerHub works only for releases.")
    .IsDependentOn("Docker-Build")
    .IsDependentOn("Docker-Test")
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

    DockerStdinLogin(username, password);

    foreach(var dockerImage in parameters.Docker.Images)
    {
        DockerPush(dockerImage, parameters);
    }

    DockerLogout();
})
.OnError(exception =>
{
    Information("Publish-DockerHub Task failed, but continuing with next Task...");
    Error(exception.Dump());
    publishingError = true;
});
