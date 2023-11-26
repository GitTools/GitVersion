namespace GitVersion.VersionCalculation.TrunkBased;

internal sealed class RemoveSemanticVersion : ITrunkBasedContextPostEnricher
{
    public void Enrich(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context) => context.SemanticVersion = null;
}
