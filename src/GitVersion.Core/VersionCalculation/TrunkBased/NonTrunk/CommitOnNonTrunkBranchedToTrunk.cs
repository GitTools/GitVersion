namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal sealed class CommitOnNonTrunkBranchedToTrunk : CommitOnNonTrunkBranchedBase
{
    // B  51 minutes ago  (HEAD -> release/1.0.x, main) <<--
    // A  59 minutes ago

    // B  58 minutes ago  (main)
    // A  59 minutes ago  (HEAD -> release/1.0.x) <<--

    // *  54 minutes ago  (main)
    // | B  56 minutes ago  (HEAD -> release/1.0.x)
    // |/
    // A  58 minutes ago <<--

    public override bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => base.MatchPrecondition(iteration, commit, context) && iteration.Configuration.IsMainline;
}
