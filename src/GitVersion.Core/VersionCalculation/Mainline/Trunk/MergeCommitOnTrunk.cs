namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class MergeCommitOnTrunk : MergeCommitOnTrunkBase
{
    // C  53 minutes ago  (HEAD -> main)
    // *  55 minutes ago <<--
    // |\
    // | B  56 minutes ago  (feature/foo)

    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is not null;
}
