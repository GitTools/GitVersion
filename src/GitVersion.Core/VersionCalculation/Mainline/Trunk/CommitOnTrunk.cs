namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class CommitOnTrunk : CommitOnTrunkBase
{
    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => !context.IsMainlineMerge && base.MatchPrecondition(iteration, commit, context);
}
