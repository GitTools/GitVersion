namespace GitVersion.VersionCalculation.Mainline;

internal sealed class RemoveSemanticVersion : IContextPostEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context) => context.SemanticVersion = null;
}
