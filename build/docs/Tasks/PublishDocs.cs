using Cake.Git;
using Cake.Wyam;
using Common.Utilities;

namespace Docs.Tasks;

[TaskName(nameof(PublishDocs))]
[TaskDescription("Published the docs changes to docs specific branch")]
[IsDependentOn(typeof(PublishDocsInternal))]
public sealed class PublishDocs : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.DirectoryExists(Paths.Docs), "Wyam documentation directory is missing");

        return shouldRun;
    }
}

[TaskName(nameof(PublishDocsInternal))]
[TaskDescription("Published the docs changes to docs specific branch")]
[IsDependentOn(typeof(Clean))]
public sealed class PublishDocsInternal : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.DirectoryExists(Paths.Docs), "Wyam documentation directory is missing");
        shouldRun &= context.ShouldRun(context.IsStableRelease || context.IsPreRelease || context.ForcePublish, $"{nameof(PublishDocs)} works only for releases.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        if (context.ForcePublish is false)
        {
            if (AnyDocsChanged(context))
            {
                context.Information("Performing a docs publish. Detected docs changes");
                PublishDocumentation(context);
            }
            else
            {
                context.Information("No docs have changed, so no need to generate documentation");
            }
        }
        else
        {
            context.Information("Performing a forced docs publish...");
            PublishDocumentation(context);
        }
    }
    private static bool AnyDocsChanged(ICakeContext context)
    {
        var sourceCommit = context.GitLogTip(Paths.Root);
        var filesChanged = context.GitDiff(Paths.Root, sourceCommit.Sha);

        const string path = "docs/";
        var docFileChanged = filesChanged.Any(file => file.OldPath.StartsWith(path) || file.Path.StartsWith(path) || file.Path.Contains("config.wyam"));
        return docFileChanged;
    }

    private static void PublishDocumentation(BuildContext context)
    {
        const string publishBranchName = "gh-pages";
        var sourceCommit = context.GitLogTip("./");

        var publishFolder = context.MakeAbsolute(Paths.ArtifactsDocs.Combine("_published").Combine(DateTime.Now.ToString("yyyyMMdd_HHmmss")));
        context.Information("Publishing Folder: {0}", publishFolder);
        context.Information("Getting publish branch...");
        context.GitClone($"https://github.com/{Constants.RepoOwner}/{Constants.Repository}", publishFolder, new GitCloneSettings
        {
            BranchName = publishBranchName
        });

        if (context.WyamSettings is not null)
        {
            context.WyamSettings.OutputPath = publishFolder;
            context.WyamSettings.NoClean = true;
            context.Wyam(context.WyamSettings);
        }

        if (!context.GitHasUncommitedChanges(publishFolder)) return;

        context.Information("Stage all changes...");
        context.GitAddAll(publishFolder);

        if (!context.GitHasStagedChanges(publishFolder)) return;

        context.Information("Commit all changes...");
        context.GitCommit(
            publishFolder,
            sourceCommit.Committer.Name,
            sourceCommit.Committer.Email,
            $"Continuous Integration Publish: {sourceCommit.Sha}\r\n{sourceCommit.Message}"
        );

        PublishToGitHub(context, publishFolder, publishBranchName);
    }

    private static void PublishToGitHub(BuildContext context, DirectoryPath publishFolder, string publishBranchName)
    {
        var token = context.Credentials?.GitHub?.Token;
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Could not resolve Github token.");
        }

        var username = context.Credentials?.GitHub?.UserName;
        if (string.IsNullOrEmpty(username))
        {
            throw new InvalidOperationException("Could not resolve Github username.");
        }

        context.Information("Pushing all changes...");

        context.GitPushBranch(publishFolder, username, token, publishBranchName);
    }
}
