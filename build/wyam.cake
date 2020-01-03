Task("Clean-Documentation")
    .Does(() =>
{
    EnsureDirectoryExists(MakeAbsolute(Directory("artifacts/temp/_PublishedDocumentation")));
});

Task("Preview-Documentation")
    .WithCriteria(() => DirectoryExists(MakeAbsolute(Directory("docs"))), "Wyam documentation directory is missing")
    .Does<BuildParameters>((parameters) =>
{
    Wyam(new WyamSettings
    {
        Recipe = "Docs",
        Theme = "Samson",
        OutputPath = MakeAbsolute(Directory("artifacts/Documentation")),
        RootPath = MakeAbsolute(Directory("docs")),
        Preview = true,
        Watch = true,
        ConfigurationFile = MakeAbsolute((FilePath)"config.wyam"),
        Settings = new Dictionary<string, object>
        {
            { "Host",  "gittools.github.io" },
            { "BaseEditUrl", "https://github.com/gittools/GitVersion/tree/master/docs/input/" },
            { "SourceFiles", MakeAbsolute(parameters.Paths.Directories.Source) + "/**/{!bin,!obj,!packages,!*.Tests,}/**/*.cs" },
            { "Title", "GitVersion" },
            { "IncludeGlobalNamespace", false }
        }
    });
});

Task("Force-Publish-Documentation")
    .IsDependentOn("Clean-Documentation")
    .WithCriteria(() => DirectoryExists(MakeAbsolute(Directory("docs"))), "Wyam documentation directory is missing")
    .Does<BuildParameters>((parameters) =>
{
    Wyam(new WyamSettings
    {
        Recipe = "Docs",
        Theme = "Samson",
        OutputPath = MakeAbsolute(Directory("artifacts/Documentation")),
        RootPath = MakeAbsolute(Directory("docs")),
        ConfigurationFile = MakeAbsolute((FilePath)"config.wyam"),
        Settings = new Dictionary<string, object>
        {
            { "BaseEditUrl", "https://github.com/gittools/GitVersion/tree/master/docs/input/" },
            { "SourceFiles", MakeAbsolute(parameters.Paths.Directories.Source) + "/**/{!bin,!obj,!packages,!*.Tests,}/**/*.cs" },
            { "Title", "GitVersion" },
            { "IncludeGlobalNamespace", false }
        }
    });

    PublishDocumentation(parameters);
});

Task("Publish-Documentation")
    .IsDependentOn("Clean-Documentation")
    .WithCriteria(() => DirectoryExists(MakeAbsolute(Directory("docs"))), "Wyam documentation directory is missing")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,       "Publish-Documentation is ran only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Publish-Documentation is ran only on AzurePipeline.")
    .WithCriteria<BuildParameters>((context, parameters) => !parameters.IsPullRequest, "Publish-Documentation works only for non-PR commits.")
    .Does<BuildParameters>((parameters) =>
{
    // Check to see if any documentation has changed
    var sourceCommit = GitLogTip("./");
    Information("Source Commit Sha: {0}", sourceCommit.Sha);
    var filesChanged = GitDiff("./", sourceCommit.Sha);
    Information("Number of changed files: {0}", filesChanged.Count);
    var docFileChanged = false;

    var wyamDocsFolderDirectoryName = "docs";

    foreach (var file in filesChanged)
    {
        var forwardSlash = '/';
        Verbose("Changed File OldPath: {0}, Path: {1}", file.OldPath, file.Path);
        if (file.OldPath.Contains(string.Format("{0}{1}", wyamDocsFolderDirectoryName, forwardSlash)) ||
            file.Path.Contains(string.Format("{0}{1}", wyamDocsFolderDirectoryName, forwardSlash)) ||
            file.Path.Contains("config.wyam"))
        {
        docFileChanged = true;
        break;
        }
    }

    if (docFileChanged)
    {
        Information("Detected that documentation files have changed, so running Wyam...");

        Wyam(new WyamSettings
        {
            Recipe = "Docs",
            Theme = "Samson",
            OutputPath = MakeAbsolute(Directory("artifacts/Documentation")),
            RootPath = MakeAbsolute(Directory("docs")),
            ConfigurationFile = MakeAbsolute((FilePath)"config.wyam"),
            Settings = new Dictionary<string, object>
            {
                { "BaseEditUrl", "https://github.com/gittools/GitVersion/tree/master/docs/input/" },
                { "SourceFiles", MakeAbsolute(parameters.Paths.Directories.Source) + "/**/{!bin,!obj,!packages,!*.Tests,}/**/*.cs" },
                { "Title", "GitVersion" },
                { "IncludeGlobalNamespace", false }
            }
        });

        PublishDocumentation(parameters);
    }
    else
    {
        Information("No documentation has changed, so no need to generate documentation");
    }
})
.OnError(exception =>
{
    Error(exception.Message);
    Information("Publish-Documentation Task failed, but continuing with next Task...");
    publishingError = true;
});

public void PublishDocumentation(BuildParameters parameters)
{
    var sourceCommit = GitLogTip("./");

    var publishFolder = MakeAbsolute(Directory("artifacts/temp/_PublishedDocumentation")).Combine(DateTime.Now.ToString("yyyyMMdd_HHmmss"));
    Information("Publishing Folder: {0}", publishFolder);
    Information("Getting publish branch...");
    GitClone("https://github.com/gittools/GitVersion", publishFolder, new GitCloneSettings{ BranchName = "gh-pages" });

    Information("Sync output files...");
    Kudu.Sync(MakeAbsolute(Directory("artifacts/Documentation")), publishFolder, new KuduSyncSettings {
        ArgumentCustomization = args=>args.Append("--ignore").AppendQuoted(".git;CNAME")
    });

    if (GitHasUncommitedChanges(publishFolder))
    {
        Information("Stage all changes...");
        GitAddAll(publishFolder);

        if (GitHasStagedChanges(publishFolder))
        {
            Information("Commit all changes...");
            GitCommit(
                publishFolder,
                sourceCommit.Committer.Name,
                sourceCommit.Committer.Email,
                string.Format("Continuous Integration Publish: {0}\r\n{1}", sourceCommit.Sha, sourceCommit.Message)
            );

            Information("Pushing all changes...");
            GitPush(publishFolder, parameters.Credentials.GitHub.Token, "x-oauth-basic", "gh-pages");
        }
    }
}
