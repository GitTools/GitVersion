using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
/// BaseVersionSource is the tag's commit.
/// Increments if the tag is not the current commit.
/// </summary>
internal sealed class TaggedCommitVersionStrategy : VersionStrategyBase
{
    private readonly IRepositoryStore repositoryStore;

    public TaggedCommitVersionStrategy(IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
        : base(versionContext) => this.repositoryStore = repositoryStore.NotNull();

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
        => Context.Configuration.VersioningMode == VersioningMode.TrunkBased ? Enumerable.Empty<BaseVersion>()
        : GetSemanticVersions(configuration.Value).Select(CreateBaseVersion);

    private IEnumerable<SemanticVersionWithTag> GetSemanticVersions(EffectiveConfiguration configuration)
    {
        var label = configuration.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);

        var semanticVersions = this.repositoryStore.GetSemanticVersions(
            configuration: Context.Configuration,
            currentBranch: Context.CurrentBranch,
            currentCommit: Context.CurrentCommit,
            trackMergeTarget: configuration.TrackMergeTarget,
            tracksReleaseBranches: configuration.TracksReleaseBranches
        ).ToArray();

        foreach (var semanticVersion in semanticVersions)
        {
            if (semanticVersion.Value.IsMatchForBranchSpecificLabel(label))
                yield return semanticVersion;
        }
    }

    private static BaseVersion CreateBaseVersion(SemanticVersionWithTag semanticVersion)
    {
        var tagCommit = semanticVersion.Tag.Commit;
        return new BaseVersion(
             $"Git tag '{semanticVersion.Tag.Name.Friendly}'", true, semanticVersion.Value, tagCommit, null
         );
    }
}
