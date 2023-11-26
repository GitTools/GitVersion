namespace GitVersion.VersionCalculation.TrunkBased;

internal interface ITrunkBasedContextPreEnricher
{
    void Enrich(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context);
}
