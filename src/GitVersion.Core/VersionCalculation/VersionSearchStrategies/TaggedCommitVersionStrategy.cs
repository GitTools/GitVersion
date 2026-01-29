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
    ILogger<TaggedCommitVersionStrategy> logger,
    Lazy<GitVersionContext> contextLazy,
    ITaggedSemanticVersionService taggedSemanticVersionService,
    IIncrementStrategyFinder incrementStrategyFinder)
    : IVersionStrategy
{
    private readonly ILogger<TaggedCommitVersionStrategy> logger = logger.NotNull();
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

        var semanticVersionTreshold = SemanticVersion.Empty;
        List<SemanticVersionWithTag> alternativeSemanticVersionsWithTag = [];
        foreach (var semanticVersion in taggedSemanticVersions)
        {
            if (!semanticVersion.Value.IsMatchForBranchSpecificLabel(label))
            {
                alternativeSemanticVersionsWithTag.Add(semanticVersion);
                continue;
            }

            var alternativeSemanticVersionMax = alternativeSemanticVersionsWithTag.Max()?.Value;
            var highestPossibleSemanticVersion = semanticVersion.Value.Increment(
                VersionField.Major, null, forceIncrement: true, alternativeSemanticVersionMax
            );
            if (highestPossibleSemanticVersion.IsLessThan(semanticVersionTreshold, includePreRelease: false))
            {
                this.logger.LogInformation(
                    "The tag '{SemanticVersion}' is skipped because it provides a lower base version than other tags.",
                    semanticVersion.Value
                );
                alternativeSemanticVersionsWithTag.Clear();
                continue;
            }

            var baseVersionSource = semanticVersion.Tag.Commit;
            var increment = incrementStrategyFinder.DetermineIncrementedField(
                currentCommit: Context.CurrentCommit,
                baseVersionSource: baseVersionSource,
                shouldIncrement: true,
                configuration: configuration.Value,
                label: label
            );
            semanticVersionTreshold = semanticVersion.Value.Increment(increment, null, forceIncrement: true);

            yield return new BaseVersion(
                $"Git tag '{semanticVersion.Tag.Name.Friendly}'", semanticVersion.Value, baseVersionSource)
            {
                Operator = new BaseVersionOperator
                {
                    Increment = increment,
                    ForceIncrement = false,
                    Label = label,
                    AlternativeSemanticVersion = alternativeSemanticVersionMax
                }
            };
            alternativeSemanticVersionsWithTag.Clear();
        }
    }
}
