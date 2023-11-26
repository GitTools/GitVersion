namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal sealed class CommitOnNonTrunkWithPreReleaseTag : CommitOnNonTrunkWithPreReleaseTagBase
{
    // B 57 minutes ago  (HEAD -> feature/foo)
    // A 58 minutes ago  (tag 1.2.3-1) <<--

    public override bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is not null;
}
