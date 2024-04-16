using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from all tags on the branch which are valid, and not newer than the current commit.
/// BaseVersionSource is the tag's commit.
/// Increments if the tag is not the current commit.
/// </summary>
internal sealed class FallbackVersionStrategy(
    Lazy<GitVersionContext> contextLazy,
    IIncrementStrategyFinder incrementStrategyFinder,
    ITaggedSemanticVersionService taggedSemanticVersionService)
    : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly IIncrementStrategyFinder incrementStrategyFinder = incrementStrategyFinder.NotNull();
    private readonly ITaggedSemanticVersionService taggedSemanticVersionService = taggedSemanticVersionService.NotNull();

    private GitVersionContext Context => contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
        => GetBaseVersionsInternal(configuration);

    private IEnumerable<BaseVersion> GetBaseVersionsInternal(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.Fallback))
            yield break;

        var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);

        var baseVersionSource = taggedSemanticVersionService.GetTaggedSemanticVersions(
            branch: Context.CurrentBranch,
            configuration: Context.Configuration,
            label: label,
            notOlderThan: Context.CurrentCommit.When,
            taggedSemanticVersion: configuration.Value.GetTaggedSemanticVersion()
        ).Select(element => element.Key).FirstOrDefault();

        var increment = incrementStrategyFinder.DetermineIncrementedField(
            currentCommit: Context.CurrentCommit,
            baseVersionSource: baseVersionSource,
            shouldIncrement: true,
            configuration: configuration.Value,
            label: label
        );

        yield return new BaseVersion()
        {
            Operator = new BaseVersionOperator()
            {
                Source = "Fallback base version",
                BaseVersionSource = baseVersionSource,
                Increment = increment,
                ForceIncrement = false,
                Label = label
            }
        };
    }
}
