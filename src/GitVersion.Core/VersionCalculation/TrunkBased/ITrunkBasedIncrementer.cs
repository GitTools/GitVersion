namespace GitVersion.VersionCalculation.TrunkBased;

internal interface ITrunkBasedIncrementer
{
    bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context);

    IEnumerable<IBaseVersionIncrement> GetIncrements(
        TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context);
}
