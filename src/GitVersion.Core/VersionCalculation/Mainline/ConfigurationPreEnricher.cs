namespace GitVersion.VersionCalculation.Mainline;

internal sealed class ConfigurationPreEnricher : IContextPreEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.EffectiveConfiguration = context.GetEffectiveConfiguration(commit);
        context.IterationConfiguration = context.GetEffectiveConfiguration(iteration);
        context.ChildConfiguration = commit.ChildIteration is { } child
            ? context.GetEffectiveConfiguration(child) : null;
    }
}
