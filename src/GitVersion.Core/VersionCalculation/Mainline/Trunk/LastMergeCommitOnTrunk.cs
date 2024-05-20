
namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class LastMergeCommitOnTrunk : MergeCommitOnTrunkBase
{
    // *  55 minutes ago  (HEAD -> main) <<--
    // |\
    // | B  56 minutes ago  (feature/foo)

    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is null;
}
