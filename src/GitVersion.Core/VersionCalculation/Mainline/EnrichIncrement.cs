using System.ComponentModel;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class EnrichIncrement : IContextPreEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
        var incrementForcedByBranch = effectiveConfiguration.Increment.ToVersionField();
        // An eligible tag on a merged mainline already releases that commit's bump message.
        var isTaggedMergedMainline = context.SemanticVersion is not null
            && iteration.Configuration.IsMainBranch == true
            && effectiveConfiguration.IsMainBranch
            && effectiveConfiguration.PreventIncrementWhenCurrentCommitTagged
            && iteration.ParentCommit?.GetEffectiveConfiguration(context.Configuration).IsMainBranch == true;
        var commitMessageIncrement = commit.IsDummy || isTaggedMergedMainline
            ? default
            : GetIncrementForcedByCommit(context, commit.Value, effectiveConfiguration);
        commit.Increment = commitMessageIncrement.Increment;
        if (commitMessageIncrement.VersionBumpNeedsToBeReset)
        {
            context.Increment = VersionField.None;
            context.SuppressBranchIncrement = true;
        }

        context.Increment = context.Increment.Consolidate(commitMessageIncrement.Increment);
        context.Increment = context.Increment.Consolidate(commit.MergedBranchIncrement);
        if (!context.SuppressBranchIncrement
            && (commit.MergedBranchIncrement is null || !effectiveConfiguration.PreventIncrementOfMergedBranch))
        {
            context.Increment = context.Increment.Consolidate(incrementForcedByBranch);
        }

        if (commit.Predecessor is not null && commit.Predecessor.BranchName != commit.BranchName)
        {
            context.Label = null;
        }

        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null, context.Environment, context.CurrentCommit);

        if (effectiveConfiguration.IsMainBranch)
        {
            context.BaseVersionSource = commit.Predecessor?.Value;
        }

        context.ForceIncrement |= effectiveConfiguration.IsMainBranch || commit.IsPredecessorTheLastCommitOnTrunk(context.Configuration);
    }

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
