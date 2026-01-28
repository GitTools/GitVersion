using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

public class VersionCalculationModule : IGitVersionModule
{
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
