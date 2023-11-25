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
internal class TrackReleaseBranchesVersionStrategy : VersionStrategyBase
{
    private readonly VersionInBranchNameVersionStrategy releaseVersionStrategy;

    private readonly IRepositoryStore repositoryStore;

    public TrackReleaseBranchesVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(versionContext)
    {
        this.repositoryStore = repositoryStore.NotNull();
        this.releaseVersionStrategy = new VersionInBranchNameVersionStrategy(repositoryStore, versionContext);
    }

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (Context.Configuration.VersioningMode == VersioningMode.TrunkBased) return Enumerable.Empty<BaseVersion>();

        return configuration.Value.TracksReleaseBranches ? ReleaseBranchBaseVersions() : Array.Empty<BaseVersion>();
    }

    private IEnumerable<BaseVersion> ReleaseBranchBaseVersions()
    {
        var releaseBranchConfig = Context.Configuration.GetReleaseBranchConfiguration();
        if (!releaseBranchConfig.Any())
            return Array.Empty<BaseVersion>();

        var releaseBranches = this.repositoryStore.GetReleaseBranches(releaseBranchConfig);

        return releaseBranches
            .SelectMany(GetReleaseVersion)
            .Select(baseVersion =>
            {
                // Need to drop branch overrides and give a bit more context about
                // where this version came from
                var source1 = "Release branch exists -> " + baseVersion.Source;
                return new BaseVersion(source1,
                    baseVersion.ShouldIncrement,
                    baseVersion.GetSemanticVersion(),
                    baseVersion.BaseVersionSource,
                    null);
            })
            .ToList();
    }

    private IEnumerable<BaseVersion> GetReleaseVersion(IBranch releaseBranch)
    {
        // Find the commit where the child branch was created.
        var baseSource = this.repositoryStore.FindMergeBase(releaseBranch, Context.CurrentBranch);
        var effectiveBranchConfiguration = Context.Configuration.GetEffectiveBranchConfiguration(releaseBranch);
        return this.releaseVersionStrategy
            .GetBaseVersions(effectiveBranchConfiguration)
            .Select(b => new BaseVersion(b.Source, true, b.GetSemanticVersion(), baseSource, b.BranchNameOverride));
    }
}
