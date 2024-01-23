using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased;

internal sealed class EnrichIncrement : ITrunkBasedContextPreEnricher
{
    public void Enrich(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        var incrementForcedByBranch = commit.GetIncrementForcedByBranch();
        var incrementForcedByCommit = commit.Increment;
        context.Increment = context.Increment.Consolidate(incrementForcedByBranch, incrementForcedByCommit);

        if (commit.Predecessor is not null && commit.Predecessor.BranchName != commit.BranchName)
            context.Label = null;
        context.Label ??= commit.Configuration.GetBranchSpecificLabel(commit.BranchName, null);

        if (commit.Configuration.IsMainBranch)
            context.BaseVersionSource = commit.Predecessor?.Value;
        context.ForceIncrement |= commit.Configuration.IsMainBranch || commit.IsPredecessorTheLastCommitOnTrunk;
    }
}
