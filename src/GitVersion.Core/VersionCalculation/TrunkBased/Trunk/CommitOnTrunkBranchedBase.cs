using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal abstract class CommitOnTrunkBranchedBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => commit.Configuration.IsMainBranch && commit.BranchName != iteration.BranchName && commit.Successor is null;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        context.BaseVersionSource = commit.Value;

        var incrementForcedByBranch = iteration.Configuration.Increment == IncrementStrategy.Inherit
            ? commit.GetIncrementForcedByBranch() : iteration.Configuration.Increment.ToVersionField();
        context.Increment = incrementForcedByBranch;

        context.Label = iteration.Configuration.GetBranchSpecificLabel(iteration.BranchName, null) ?? context.Label;
        context.ForceIncrement = true;

        yield return new BaseVersionOperator()
        {
            Source = GetType().Name,
            BaseVersionSource = context.BaseVersionSource,
            Increment = context.Increment,
            ForceIncrement = context.ForceIncrement,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
        };
    }
}
