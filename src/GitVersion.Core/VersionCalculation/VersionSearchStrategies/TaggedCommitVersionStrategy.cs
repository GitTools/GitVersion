using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
/// BaseVersionSource is the tag's commit.
/// Increments if the tag is not the current commit.
/// </summary>
internal sealed class TaggedCommitVersionStrategy(
    Lazy<GitVersionContext> contextLazy,
    ITaggedSemanticVersionRepository taggedSemanticVersionRepository,
    IIncrementStrategyFinder incrementStrategyFinder)
    : IVersionStrategy
{
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly IIncrementStrategyFinder incrementStrategyFinder = incrementStrategyFinder.NotNull();

    private GitVersionContext Context => contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
        => GetBaseVersionsInternal(configuration);

    private IEnumerable<BaseVersion> GetBaseVersionsInternal(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.TaggedCommit))
            yield break;

        var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);
        var taggedSemanticVersions = taggedSemanticVersionRepository
            .GetAllTaggedSemanticVersions(Context.Configuration, configuration.Value, Context.CurrentBranch, label, Context.CurrentCommit.When)
            .SelectMany(element => element)
            .Distinct().ToArray();

        foreach (var semanticVersionWithTag in taggedSemanticVersions)
        {
            var baseVersionSource = semanticVersionWithTag.Tag.Commit;
            var increment = incrementStrategyFinder.DetermineIncrementedField(
                currentCommit: Context.CurrentCommit,
                baseVersionSource: baseVersionSource,
                shouldIncrement: true,
                configuration: configuration.Value,
                label: label
            );
            yield return new BaseVersion(
                $"Git tag '{semanticVersionWithTag.Tag.Name.Friendly}'", semanticVersionWithTag.Value, baseVersionSource)
            {
                Operator = new BaseVersionOperator()
                {
                    Increment = increment,
                    ForceIncrement = false,
                    Label = label
                }
            };
        }
    }
}
