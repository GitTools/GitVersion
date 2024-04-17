using System.ComponentModel;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.TrunkBased;

internal sealed class EnrichIncrement : ITrunkBasedContextPreEnricher
{
    public void Enrich(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        var effectiveConfiguration = commit.GetEffectiveConfiguration(context.Configuration);
        var incrementForcedByBranch = effectiveConfiguration.Increment.ToVersionField();
        var incrementForcedByCommit = GetIncrementForcedByCommit(context, commit.Value, effectiveConfiguration);
        commit.Increment = incrementForcedByCommit;
        context.Increment = context.Increment.Consolidate(incrementForcedByBranch, incrementForcedByCommit);

        if (commit.Predecessor is not null && commit.Predecessor.BranchName != commit.BranchName)
            context.Label = null;
        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null);

        if (effectiveConfiguration.IsMainBranch)
            context.BaseVersionSource = commit.Predecessor?.Value;
        context.ForceIncrement |= effectiveConfiguration.IsMainBranch || commit.IsPredecessorTheLastCommitOnTrunk(context.Configuration);
    }

    private static VersionField GetIncrementForcedByCommit(
        TrunkBasedContext context, ICommit commit, EffectiveConfiguration configuration)
    {
        context.NotNull();
        commit.NotNull();
        configuration.NotNull();

        return configuration.CommitMessageIncrementing switch
        {
            CommitMessageIncrementMode.Enabled
                => context.IncrementStrategyFinder.GetIncrementForcedByCommit(commit, context.Configuration),
            CommitMessageIncrementMode.Disabled => VersionField.None,
            CommitMessageIncrementMode.MergeMessageOnly => commit.IsMergeCommit()
                ? context.IncrementStrategyFinder.GetIncrementForcedByCommit(commit, context.Configuration) : VersionField.None,
            _ => throw new InvalidEnumArgumentException(
                nameof(configuration.CommitMessageIncrementing), (int)configuration.CommitMessageIncrementing, typeof(CommitMessageIncrementMode)
            )
        };
    }
}
