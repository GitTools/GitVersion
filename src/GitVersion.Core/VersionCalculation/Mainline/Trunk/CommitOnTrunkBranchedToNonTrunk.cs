namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class CommitOnTrunkBranchedToNonTrunk(IEnvironment environment) : CommitOnTrunkBranchedBase(environment)
{
    // B  51 minutes ago  (HEAD -> feature/foo, main) <<--
    // A  59 minutes ago

    // B  58 minutes ago  (main)
    // A  59 minutes ago  (HEAD -> feature/foo) <<--

    // *  54 minutes ago  (main)
    // | B  56 minutes ago  (HEAD -> feature/foo)
    // |/
    // A  58 minutes ago <<--

    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => !context.IsMainlineMerge && base.MatchPrecondition(iteration, commit, context)
           && !context.IterationConfiguration.IsMainBranch;
}
