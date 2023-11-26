using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal abstract class CommitOnTrunkBranchedBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.Configuration.IsMainline && commit.BranchName != iteration.BranchName && commit.Successor is null;

    public virtual IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        context.BaseVersionSource = commit.Value;

        var incrementForcedByBranch = iteration.Configuration.Increment == IncrementStrategy.Inherit
            ? commit.GetIncrementForcedByBranch() : iteration.Configuration.Increment.ToVersionField();
        context.Increment = context.Increment.Consolidate(incrementForcedByBranch);

        context.Label = iteration.Configuration.GetBranchSpecificLabel(iteration.BranchName, null) ?? context.Label;
        context.ForceIncrement = true;

        yield return BaseVersionV2.ShouldIncrementTrue(
            source: GetType().Name,
            baseVersionSource: context.BaseVersionSource,
            increment: context.Increment,
            label: context.Label,
            forceIncrement: context.ForceIncrement,
            alternativeSemanticVersion: context.AlternativeSemanticVersions.Max()
        );
    }
}
