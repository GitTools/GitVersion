namespace GitVersion.VersionCalculation.TrunkBased;

internal interface ITrunkBasedIncrementer
{
    bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context);

    IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context);
}
