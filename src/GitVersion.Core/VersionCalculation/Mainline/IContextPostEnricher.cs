namespace GitVersion.VersionCalculation.Mainline;

internal interface IContextPostEnricher
{
    void Enrich(MainlineCommit commit, MainlineContext context);
}
