using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.VersionCalculation;

public class VersionCalculationModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(new VersionStrategyModule());

        services.AddSingleton<IVariableProvider, VariableProvider>();
        services.AddSingleton<IBaseVersionCalculator, BaseVersionCalculator>();
        services.AddSingleton<IMainlineVersionCalculator, MainlineVersionCalculator>();
        services.AddSingleton<INextVersionCalculator, NextVersionCalculator>();
        services.AddSingleton<IIncrementStrategyFinder, IncrementStrategyFinder>();
    }
}
