using Cake.Common.Tools.GitReleaseManager;
using Cake.Common.Tools.GitReleaseManager.Create;
using Common.Utilities;

namespace Release.Tasks;

[TaskName(nameof(PublishRelease))]
[TaskDescription("Publish release")]
[IsDependentOn(typeof(PublishReleaseInternal))]

public class PublishRelease : FrostingTask<BuildContext>;

[TaskName(nameof(PublishReleaseInternal))]
[TaskDescription("Publish release")]
public class PublishReleaseInternal : FrostingTask<BuildContext>
{
    public override bool ShouldRun(BuildContext context)
    {
        var shouldRun = true;
        shouldRun &= context.ShouldRun(context.IsGitHubActionsBuild, $"{nameof(PublishRelease)} works only on GitHub Actions.");
        shouldRun &= context.ShouldRun(context.IsTaggedRelease || context.IsTaggedPreRelease, $"{nameof(PublishRelease)} works only for tagged releases.");

        return shouldRun;
    }

    public override void Run(BuildContext context)
    {
        var token = context.Credentials?.GitHub?.Token;
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Could not resolve GitHub Token.");
        }

        var archives = context.GetFiles(Paths.Native + "/*.{tar.gz,zip}").Select(x => x.ToString()).ToList();
        context.Information("Archives count: " + archives.Count);

        var assets = string.Join(",", archives);

        var version = context.Version?.SemVersion;
        if (version is null)
        {
            return;
        }

        var sha = context.ExecuteCommand("git", "rev-parse HEAD").Single();
        var publication = ReleasePublication.Create(version, sha);

        // GRM 0.20 uses Milestone as the tag, even when Name differs. A beta
        // therefore keeps the reviewed notes on its already-published release.
        if (!publication.Prerelease)
        {
            context.GitReleaseManagerCreate(token, Constants.RepoOwner, Constants.Repository, new GitReleaseManagerCreateSettings
            {
                Milestone = publication.Milestone,
                Name = publication.Name,
                Prerelease = publication.Prerelease,
                TargetCommitish = publication.TargetCommitish
            });
        }

        context.GitReleaseManagerAddAssets(token, Constants.RepoOwner, Constants.Repository, publication.Name, assets);
        if (publication.CloseMilestone)
        {
            context.GitReleaseManagerClose(token, Constants.RepoOwner, Constants.Repository, publication.Milestone);
        }
    }
}
