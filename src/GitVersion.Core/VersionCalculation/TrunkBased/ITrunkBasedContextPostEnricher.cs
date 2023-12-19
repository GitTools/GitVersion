namespace GitVersion.VersionCalculation.TrunkBased;

internal interface ITrunkBasedContextPostEnricher
{
    void Enrich(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context);
}
