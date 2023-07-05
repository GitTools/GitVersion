using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.VersionCalculation;

public class VersionCalculationModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(new VersionStrategyModule());

        services.AddSingleton<IVariableProvider, VariableProvider>();
        services.AddSingleton<IMainlineVersionCalculator, MainlineVersionCalculator>();
        services.AddSingleton<ITrunkBasedVersionCalculator, TrunkBasedVersionCalculator>();
        services.AddSingleton<IContinuousDeploymentVersionCalculator, ContinuousDeploymentVersionCalculator>();
        services.AddSingleton<IContinuousDeliveryVersionCalculator, ContinuousDeliveryVersionCalculator>();
        services.AddSingleton<IManualDeploymentVersionCalculator, ManualDeploymentVersionCalculator>();
        services.AddSingleton<INextVersionCalculator, NextVersionCalculator>();
        services.AddSingleton<IIncrementStrategyFinder, IncrementStrategyFinder>();
        services.AddSingleton<IEffectiveBranchConfigurationFinder, EffectiveBranchConfigurationFinder>();
    }
}
