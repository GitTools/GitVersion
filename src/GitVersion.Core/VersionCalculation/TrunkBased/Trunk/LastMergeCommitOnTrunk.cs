
namespace GitVersion.VersionCalculation.TrunkBased.Trunk;

internal sealed class LastMergeCommitOnTrunk : MergeCommitOnTrunkBase
{
    // *  55 minutes ago  (HEAD -> main) <<--
    // |\
    // | B  56 minutes ago  (feature/foo)

    public override bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is null;
}
