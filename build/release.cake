Task("Release-Notes")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledPublishRelease, "Release-Notes was disabled.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,    "Release notes are generated only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsReleasingCI,         "Release notes are generated only on Releasing CI.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease(),     "Release notes are generated only for stable releases.")
    .Does<BuildParameters>((parameters) =>
{
    var token = parameters.Credentials.GitHub.Token;
    if(string.IsNullOrEmpty(token)) {
        throw new InvalidOperationException("Could not resolve Github token.");
    }

    var zipFiles = GetFiles(parameters.Paths.Directories.ArtifactsRoot.Combine("native") + "/*.tar.gz").Select(x => x.ToString());
    var assets = string.Join(",", zipFiles);

    Information("zip count: " + zipFiles.Count());

    var repoOwner = BuildParameters.MainRepoOwner;
    var repository = BuildParameters.MainRepoName;
    GitReleaseManagerCreate(token, repoOwner, repository, new GitReleaseManagerCreateSettings {
        Milestone         = parameters.Version.Milestone,
        Name              = parameters.Version.Milestone,
        Prerelease        = false,
        TargetCommitish   = "master"
    });

    GitReleaseManagerAddAssets(token, repoOwner, repository, parameters.Version.Milestone, assets);
    GitReleaseManagerClose(token, repoOwner, repository, parameters.Version.Milestone);

}).ReportError(exception =>
{
    Error(exception.Dump());
});
