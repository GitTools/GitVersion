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
        var (incrementForcedByCommit, suppressBranchIncrement) = commit.IsDummy
            ? (VersionField.None, false)
            : GetIncrementForcedByCommit(context, commit.Value, effectiveConfiguration);
        commit.Increment = incrementForcedByCommit;
        suppressBranchIncrement |= context.SuppressBranchIncrement;
        context.SuppressBranchIncrement = suppressBranchIncrement;
        context.Increment = context.Increment.Consolidate(incrementForcedByCommit);
        if (!suppressBranchIncrement)
        {
            context.Increment = context.Increment.Consolidate(incrementForcedByBranch);
        }

        if (commit.Predecessor is not null && commit.Predecessor.BranchName != commit.BranchName)
        {
            context.Label = null;
        }

        context.Label ??= effectiveConfiguration.GetBranchSpecificLabel(commit.BranchName, null, context.Environment);

        if (effectiveConfiguration.IsMainBranch)
        {
            context.BaseVersionSource = commit.Predecessor?.Value;
        }

        context.ForceIncrement |= effectiveConfiguration.IsMainBranch || commit.IsPredecessorTheLastCommitOnTrunk(context.Configuration);
    }

    private static (VersionField Increment, bool SuppressBranchIncrement) GetIncrementForcedByCommit(
        MainlineContext context, ICommit commit, EffectiveConfiguration configuration)
    {
        context.NotNull();
        commit.NotNull();
        configuration.NotNull();

        return configuration.CommitMessageIncrementing switch
        {
            CommitMessageIncrementMode.Enabled => (
                context.IncrementStrategyFinder.GetIncrementForcedByCommit(commit, context.Configuration),
                IsVersionBumpReset(commit, configuration.VersionBumpResetMessage)
            ),
            CommitMessageIncrementMode.Disabled => (VersionField.None, false),
            CommitMessageIncrementMode.MergeMessageOnly => commit.IsMergeCommit
                ? (
                    context.IncrementStrategyFinder.GetIncrementForcedByCommit(commit, context.Configuration),
                    IsVersionBumpReset(commit, configuration.VersionBumpResetMessage)
                )
                : (VersionField.None, false),
            _ => throw new InvalidEnumArgumentException(
                nameof(configuration.CommitMessageIncrementing), (int)configuration.CommitMessageIncrementing, typeof(CommitMessageIncrementMode)
            )
        };
    }

    private static bool IsVersionBumpReset(ICommit commit, string? pattern)
    {
        var regex = pattern is null
            ? RegexPatterns.VersionCalculation.DefaultVersionBumpResetRegex
            : RegexPatterns.Cache.GetOrAdd(pattern);
        return regex.IsMatch(commit.Message);
    }
}
