namespace GitVersion.VersionCalculation.Mainline;

internal sealed class ConfigurationPreEnricher(
    MainlineConfigurationResolver configurationResolver)
    : IContextPreEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.EffectiveConfiguration = configurationResolver.GetEffectiveConfiguration(commit);
        context.IterationConfiguration = configurationResolver.GetEffectiveConfiguration(iteration);
        context.ChildConfiguration = commit.ChildIteration is { } child
            ? configurationResolver.GetEffectiveConfiguration(child) : null;
    }
}
