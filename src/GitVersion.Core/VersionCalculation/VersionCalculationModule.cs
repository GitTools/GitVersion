using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.VersionCalculation;

public class VersionCalculationModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(new VersionStrategyModule());

        services.AddSingleton<IVariableProvider, VariableProvider>();
        services.AddSingleton<IVersionModeCalculator, MainlineVersionCalculator>();
        services.AddSingleton<IVersionModeCalculator, ContinuousDeploymentVersionCalculator>();
        services.AddSingleton<IVersionModeCalculator, ContinuousDeliveryVersionCalculator>();
        services.AddSingleton<IVersionModeCalculator, ManualDeploymentVersionCalculator>();
        services.AddSingleton<INextVersionCalculator, NextVersionCalculator>();
        services.AddSingleton<IIncrementStrategyFinder, IncrementStrategyFinder>();
        services.AddSingleton<IEffectiveBranchConfigurationFinder, EffectiveBranchConfigurationFinder>();
    }
}
