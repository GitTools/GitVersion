namespace GitVersion.VersionCalculation.Mainline;

internal interface IContextPreEnricher
{
    void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context);
}
