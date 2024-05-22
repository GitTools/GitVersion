namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class CommitOnTrunkWithStableTag : CommitOnTrunkWithStableTagBase
{
    // B  58 minutes ago  (HEAD -> main)
    // A  59 minutes ago  (tag 0.2.0) <<--

    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => base.MatchPrecondition(iteration, commit, context) && commit.Successor is not null;
}
