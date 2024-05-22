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
    ITaggedSemanticVersionService taggedSemanticVersionService,
    IIncrementStrategyFinder incrementStrategyFinder)
    : IVersionStrategy
{
    private readonly ITaggedSemanticVersionService taggedSemanticVersionService = taggedSemanticVersionService.NotNull();
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

        var taggedSemanticVersions = taggedSemanticVersionService.GetTaggedSemanticVersions(
            branch: Context.CurrentBranch,
            configuration: Context.Configuration,
            label: null,
            notOlderThan: Context.CurrentCommit.When,
            taggedSemanticVersion: configuration.Value.GetTaggedSemanticVersion()
        ).SelectMany(elements => elements).Distinct().ToArray();

        var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);

        List<SemanticVersionWithTag> alternativeSemanticVersionsWithTag = [];
        foreach (var semanticVersionWithTag in taggedSemanticVersions)
        {
            if (!semanticVersionWithTag.Value.IsMatchForBranchSpecificLabel(label))
            {
                alternativeSemanticVersionsWithTag.Add(semanticVersionWithTag);
                continue;
            }

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
                    Label = label,
                    AlternativeSemanticVersion = alternativeSemanticVersionsWithTag.Max()?.Value
                }
            };
            alternativeSemanticVersionsWithTag.Clear();
        }
    }
}
