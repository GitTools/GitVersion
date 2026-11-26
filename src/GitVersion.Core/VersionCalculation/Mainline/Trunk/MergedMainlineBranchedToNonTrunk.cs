namespace GitVersion.VersionCalculation.Mainline.Trunk;

internal sealed class MergedMainlineBranchedToNonTrunk(
    IEnvironment environment, MergedMainlineVersionPolicy sourceVersions)
    : CommitOnTrunkBranchedBase(environment)
{
    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.IsMainlineMerge && base.MatchPrecondition(iteration, commit, context)
            && !context.IterationConfiguration.IsMainBranch;

    protected override IBaseVersionIncrement CreateIncrement(
        BaseVersionOperator operation, MainlineCommit commit, MainlineContext context)
        => sourceVersions.CreateIncrement(operation, commit, context);
}
