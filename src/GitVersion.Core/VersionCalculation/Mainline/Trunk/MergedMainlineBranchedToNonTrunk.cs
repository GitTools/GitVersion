namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class MergedMainlineBranchedToNonTrunk : CommitOnTrunkBranchedBase
{
    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.IsMainlineMerge && base.MatchPrecondition(iteration, commit, context)
            && !context.GetEffectiveConfiguration(iteration).IsMainBranch;

    protected override IBaseVersionIncrement CreateIncrement(
        BaseVersionOperator operation, MainlineCommit commit, MainlineContext context)
        => MergedMainlineVersionPolicy.CreateIncrement(operation, commit, context);
}
