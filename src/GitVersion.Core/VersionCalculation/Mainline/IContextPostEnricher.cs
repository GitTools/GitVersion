namespace GitVersion.VersionCalculation.Mainline;

internal interface IContextPostEnricher
{
    void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context);
}
