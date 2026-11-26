using GitVersion.VersionCalculation.Mainline.Trunk;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class MergedMainlineIncrementer(
    MainlineHistory history, IEnvironment environment, MergedMainlineVersionPolicy sourceVersions)
    : CommitOnTrunkBase(history, environment)
{
    public override bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => context.IsMainlineMerge && base.MatchPrecondition(iteration, commit, context);

    protected override IBaseVersionIncrement CreateIncrement(
        BaseVersionOperator operation, MainlineCommit commit, MainlineContext context)
        => sourceVersions.CreateIncrement(operation, commit, context);
}
