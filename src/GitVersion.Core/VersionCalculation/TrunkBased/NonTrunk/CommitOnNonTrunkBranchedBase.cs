using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal abstract class CommitOnNonTrunkBranchedBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => !commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch && commit.BranchName != iteration.BranchName && commit.Successor is null;

    public virtual IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        context.BaseVersionSource = commit.Value;

        var incrementForcedByBranch = iteration.Configuration.Increment == IncrementStrategy.Inherit
            ? commit.GetIncrementForcedByBranch(context.Configuration) : iteration.Configuration.Increment.ToVersionField();
        context.Increment = context.Increment.Consolidate(incrementForcedByBranch);

        var iterationEffectiveConfiguration = iteration.GetEffectiveConfiguration(context.Configuration);
        context.Label = iterationEffectiveConfiguration.GetBranchSpecificLabel(iteration.BranchName, null) ?? context.Label;
        context.ForceIncrement = true;

        yield return new BaseVersionOperator()
        {
            Source = GetType().Name,
            BaseVersionSource = null,
            Increment = VersionField.None,
            ForceIncrement = false,
            Label = context.Label,
            AlternativeSemanticVersion = context.AlternativeSemanticVersions.Max()
        };
    }
}
