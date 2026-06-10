using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>Registers the version-calculation services including version strategies, variable provider, deployment-mode calculators, and increment strategy finder.</summary>
public class VersionCalculationModule : IGitVersionModule
{
    /// <summary>Registers all version-calculation services into the DI container.</summary>
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(new VersionStrategyModule());

        services.AddSingleton<IVariableProvider, VariableProvider>();
        services.AddSingleton<IDeploymentModeCalculator, ContinuousDeploymentVersionCalculator>();
        services.AddSingleton<IDeploymentModeCalculator, ContinuousDeliveryVersionCalculator>();
        services.AddSingleton<IDeploymentModeCalculator, ManualDeploymentVersionCalculator>();
        services.AddSingleton<INextVersionCalculator, NextVersionCalculator>();
        services.AddSingleton<IIncrementStrategyFinder, IncrementStrategyFinder>();
        services.AddSingleton<IEffectiveBranchConfigurationFinder, EffectiveBranchConfigurationFinder>();
    }
}
