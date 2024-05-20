namespace GitVersion.VersionCalculation.Mainline;

internal interface IIncrementer
{
    bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context);

    IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context);
}
