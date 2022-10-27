using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;

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
    private readonly VersionInBranchNameVersionStrategy releaseVersionStrategy;
    private readonly TaggedCommitVersionStrategy taggedCommitVersionStrategy;

    private readonly IRepositoryStore repositoryStore;

    public TrackReleaseBranchesVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(versionContext)
    {
        this.repositoryStore = repositoryStore.NotNull();
        this.releaseVersionStrategy = new VersionInBranchNameVersionStrategy(repositoryStore, versionContext);
        this.taggedCommitVersionStrategy = new TaggedCommitVersionStrategy(repositoryStore, versionContext);
    }

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration) =>
        configuration.Value.TracksReleaseBranches ? ReleaseBranchBaseVersions().Union(MainTagsVersions()) : Array.Empty<BaseVersion>();

    private IEnumerable<BaseVersion> MainTagsVersions()
    {
        var configuration = Context.Configuration;
        var mainBranch = this.repositoryStore.FindMainBranch(configuration);

        return mainBranch != null
            ? this.taggedCommitVersionStrategy.GetTaggedVersions(mainBranch, null)
            : Array.Empty<BaseVersion>();
    }

    private IEnumerable<BaseVersion> ReleaseBranchBaseVersions()
    {
        var releaseBranchConfig = Context.Configuration.GetReleaseBranchConfiguration();
        if (!releaseBranchConfig.Any())
            return Array.Empty<BaseVersion>();

        var releaseBranches = this.repositoryStore.GetReleaseBranches(releaseBranchConfig);

        return releaseBranches
            .SelectMany(b => GetReleaseVersion(b))
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

    private IEnumerable<BaseVersion> GetReleaseVersion(IBranch releaseBranch)
    {
        // Find the commit where the child branch was created.
        var baseSource = this.repositoryStore.FindMergeBase(releaseBranch, Context.CurrentBranch);
        var configuration = Context.GetEffectiveConfiguration(releaseBranch);
        return this.releaseVersionStrategy
            .GetBaseVersions(new(releaseBranch, configuration))
            .Select(b => new BaseVersion(b.Source, true, b.SemanticVersion, baseSource, b.BranchNameOverride));
    }
}
