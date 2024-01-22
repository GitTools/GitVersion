using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
/// BaseVersionSource is the tag's commit.
/// Increments if the tag is not the current commit.
/// </summary>
internal sealed class TaggedCommitVersionStrategy : VersionStrategyBase
{
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository;

    public TaggedCommitVersionStrategy(ITaggedSemanticVersionRepository taggedSemanticVersionRepository, Lazy<GitVersionContext> versionContext)
        : base(versionContext) => this.taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
        => Context.Configuration.VersioningMode == VersioningMode.TrunkBased ? []
        : GetTaggedSemanticVersions(configuration).Select(CreateBaseVersion);

    private IEnumerable<SemanticVersionWithTag> GetTaggedSemanticVersions(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);
        var taggedSemanticVersions = taggedSemanticVersionRepository
            .GetAllTaggedSemanticVersions(Context.CurrentBranch, configuration.Value).SelectMany(element => element)
            .Distinct().ToArray();

        foreach (var semanticVersion in taggedSemanticVersions)
        {
            if (semanticVersion.Value.IsMatchForBranchSpecificLabel(label))
            {
                yield return semanticVersion;
            }
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
