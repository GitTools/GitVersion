using System.ComponentModel;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class EnrichIncrement(
    IIncrementStrategyFinder incrementStrategyFinder, IGitVersionConfiguration configuration,
    MainlineConfigurationResolver configurationResolver, IEnvironment environment)
    : IContextPreEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        var effectiveConfiguration = context.EffectiveConfiguration;
        var incrementForcedByBranch = effectiveConfiguration.Increment.ToVersionField();
        // An eligible tag on a merged mainline already releases that commit's bump message.
        var isTaggedMergedMainline = context.SemanticVersion is not null
            && iteration.Configuration.IsMainBranch == true
            && effectiveConfiguration.IsMainBranch
            && effectiveConfiguration.PreventIncrementWhenCurrentCommitTagged
            && iteration.GetBranchedTo() is { } receivingMerge
            && configurationResolver.GetEffectiveConfiguration(receivingMerge).IsMainBranch;
        var commitMessageIncrement = commit.IsDummy || isTaggedMergedMainline
            ? default
            : GetIncrementForcedByCommit(commit.Value, effectiveConfiguration);
        context.CommitIncrement = commitMessageIncrement.Increment;
        if (commitMessageIncrement.VersionBumpNeedsToBeReset)
        {
            context.Increment = VersionField.None;
            context.SuppressBranchIncrement = true;
        }

        context.Increment = context.Increment.Consolidate(commitMessageIncrement.Increment);
        var mergedBranchIncrement = GetMergedBranchIncrement(context);
        context.Increment = context.Increment.Consolidate(mergedBranchIncrement);
        if (!context.SuppressBranchIncrement
            && (mergedBranchIncrement is null || !effectiveConfiguration.PreventIncrementOfMergedBranch))
        {
            context.Increment = context.Increment.Consolidate(incrementForcedByBranch);
        }

        if (context.GetPredecessor(commit) is { } predecessor && predecessor.BranchName != commit.BranchName)
        {
            context.Label = null;
        }

        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null, environment, context.CurrentCommit);

        if (effectiveConfiguration.IsMainBranch)
        {
            context.BaseVersionSource = context.GetPredecessor(commit)?.Value;
        }

        context.ForceIncrement |= effectiveConfiguration.IsMainBranch
            || context.GetPredecessor(commit) is { } previous
                && configurationResolver.GetEffectiveConfiguration(previous).IsMainBranch;
    }

    private static VersionField? GetMergedBranchIncrement(MainlineContext context)
        => context.SemanticVersion is null && context.EffectiveConfiguration.IsMainBranch
            && context.ChildConfiguration?.IsMainBranch == true ? VersionField.None : null;

    private CommitMessageIncrement GetIncrementForcedByCommit(
        ICommit commit, EffectiveConfiguration effectiveConfiguration)
    {
        commit.NotNull();
        effectiveConfiguration.NotNull();

        return effectiveConfiguration.CommitMessageIncrementing switch
        {
            CommitMessageIncrementMode.Enabled
                => incrementStrategyFinder.GetIncrementForcedByCommit(commit, configuration),
            CommitMessageIncrementMode.Disabled => default,
            CommitMessageIncrementMode.MergeMessageOnly => commit.IsMergeCommit
                ? incrementStrategyFinder.GetIncrementForcedByCommit(commit, configuration)
                : default,
            _ => throw new InvalidEnumArgumentException(
                nameof(effectiveConfiguration.CommitMessageIncrementing),
                (int)effectiveConfiguration.CommitMessageIncrementing, typeof(CommitMessageIncrementMode)
            )
        };
    }
}
