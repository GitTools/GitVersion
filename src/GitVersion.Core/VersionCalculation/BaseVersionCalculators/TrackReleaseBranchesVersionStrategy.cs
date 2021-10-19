using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Active only when the branch is marked as IsDevelop.
/// Two different algorithms (results are merged):
/// <para>
/// Using <see cref="VersionInBranchNameVersionStrategy"/>:
/// Version is that of any child branches marked with IsReleaseBranch (except if they have no commits of their own).
/// BaseVersionSource is the commit where the child branch was created.
/// Always increments.
/// </para>
/// <para>
/// Using <see cref="TaggedCommitVersionStrategy"/>:
/// Version is extracted from all tags on the <c>main</c> branch which are valid.
/// BaseVersionSource is the tag's commit (same as base strategy).
/// Increments if the tag is not the current commit (same as base strategy).
/// </para>
/// </summary>
public class TrackReleaseBranchesVersionStrategy : VersionStrategyBase
{
    private readonly IRepositoryStore repositoryStore;
    private readonly VersionInBranchNameVersionStrategy releaseVersionStrategy;
    private readonly TaggedCommitVersionStrategy taggedCommitVersionStrategy;

    public TrackReleaseBranchesVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(versionContext)
    {
        this.repositoryStore = repositoryStore ?? throw new ArgumentNullException(nameof(repositoryStore));

        this.releaseVersionStrategy = new VersionInBranchNameVersionStrategy(repositoryStore, versionContext);
        this.taggedCommitVersionStrategy = new TaggedCommitVersionStrategy(repositoryStore, versionContext);
    }

    public override IEnumerable<BaseVersion> GetVersions()
    {
        if (Context.Configuration?.TracksReleaseBranches == true)
        {
            return ReleaseBranchBaseVersions().Union(MainTagsVersions());
        }

        return Array.Empty<BaseVersion>();
    }

    private IEnumerable<BaseVersion> MainTagsVersions()
    {
        var main = this.repositoryStore.FindBranch(Config.MainBranchKey);
        return main != null ? this.taggedCommitVersionStrategy.GetTaggedVersions(main, null) : Array.Empty<BaseVersion>();
    }



    private IEnumerable<BaseVersion> ReleaseBranchBaseVersions()
    {
        var releaseBranchConfig = Context.FullConfiguration?.GetReleaseBranchConfig();
        if (releaseBranchConfig.Any())
        {
            var releaseBranches = this.repositoryStore.GetReleaseBranches(releaseBranchConfig);

            return releaseBranches
                .SelectMany(b => GetReleaseVersion(Context, b))
                .Select(baseVersion =>
                {
                    // Need to drop branch overrides and give a bit more context about
                    // where this version came from
                    var source1 = "Release branch exists -> " + baseVersion.Source;
                    return new BaseVersion(source1,
                        baseVersion.ShouldIncrement,
                        baseVersion.SemanticVersion,
                        baseVersion.BaseVersionSource,
                        null);
                })
                .ToList();
        }
        return Array.Empty<BaseVersion>();
    }

    private IEnumerable<BaseVersion> GetReleaseVersion(GitVersionContext context, IBranch releaseBranch)
    {
        var tagPrefixRegex = context.Configuration?.GitTagPrefix;

        // Find the commit where the child branch was created.
        var baseSource = this.repositoryStore.FindMergeBase(releaseBranch, context.CurrentBranch);
        if (Equals(baseSource, context.CurrentCommit))
        {
            // Ignore the branch if it has no commits.
            return Array.Empty<BaseVersion>();
        }

        return this.releaseVersionStrategy
            .GetVersions(tagPrefixRegex, releaseBranch)
            .Select(b => new BaseVersion(b.Source, true, b.SemanticVersion, baseSource, b.BranchNameOverride));
    }
}
