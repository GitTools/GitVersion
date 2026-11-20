using GitVersion.VersionCalculation.Mainline.Trunk;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class MergedMainlineIncrementer : CommitOnTrunkBase
{
    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.IsMainlineMerge && base.MatchPrecondition(iteration, commit, context);

    protected override IBaseVersionIncrement CreateIncrement(
        BaseVersionOperator operation, MainlineCommit commit, MainlineContext context)
        => MergedMainlineVersionPolicy.CreateIncrement(operation, commit, context);
}
