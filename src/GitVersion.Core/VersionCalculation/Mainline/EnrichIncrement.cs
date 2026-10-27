using System.ComponentModel;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class EnrichIncrement : IContextPreEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        var effectiveConfiguration = context.GetEffectiveConfiguration(commit);
        var incrementForcedByBranch = effectiveConfiguration.Increment.ToVersionField();
        // An eligible tag on a merged mainline already releases that commit's bump message.
        var isTaggedMergedMainline = context.SemanticVersion is not null
            && iteration.Configuration.IsMainBranch == true
            && effectiveConfiguration.IsMainBranch
            && effectiveConfiguration.PreventIncrementWhenCurrentCommitTagged
            && iteration.GetBranchedTo() is { } receivingMerge
            && context.GetEffectiveConfiguration(receivingMerge).IsMainBranch;
        var commitMessageIncrement = commit.IsDummy || isTaggedMergedMainline
            ? default
            : GetIncrementForcedByCommit(context, commit.Value, effectiveConfiguration);
        context.CommitIncrement = commitMessageIncrement.Increment;
        if (commitMessageIncrement.VersionBumpNeedsToBeReset)
        {
            context.Increment = VersionField.None;
            context.SuppressBranchIncrement = true;
        }

        context.Increment = context.Increment.Consolidate(commitMessageIncrement.Increment);
        var mergedBranchIncrement = GetMergedBranchIncrement(commit, context);
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

        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null, context.Environment, context.CurrentCommit);

        if (effectiveConfiguration.IsMainBranch)
        {
            context.BaseVersionSource = context.GetPredecessor(commit)?.Value;
        }

        context.ForceIncrement |= effectiveConfiguration.IsMainBranch
            || context.GetPredecessor(commit) is { } previous
                && context.GetEffectiveConfiguration(previous).IsMainBranch;
    }

    private static VersionField? GetMergedBranchIncrement(MainlineCommit commit, MainlineContext context)
        => context.SemanticVersion is null && context.IsMergedMainline(commit) ? VersionField.None : null;

    private static CommitMessageIncrement GetIncrementForcedByCommit(
        MainlineContext context, ICommit commit, EffectiveConfiguration configuration)
    {
        context.NotNull();
        commit.NotNull();
        configuration.NotNull();

        return configuration.CommitMessageIncrementing switch
        {
            CommitMessageIncrementMode.Enabled
                => context.IncrementStrategyFinder.GetIncrementForcedByCommit(commit, context.Configuration),
            CommitMessageIncrementMode.Disabled => default,
            CommitMessageIncrementMode.MergeMessageOnly => commit.IsMergeCommit
                ? context.IncrementStrategyFinder.GetIncrementForcedByCommit(commit, context.Configuration)
                : default,
            _ => throw new InvalidEnumArgumentException(
                nameof(configuration.CommitMessageIncrementing), (int)configuration.CommitMessageIncrementing, typeof(CommitMessageIncrementMode)
            )
        };
    }
}
