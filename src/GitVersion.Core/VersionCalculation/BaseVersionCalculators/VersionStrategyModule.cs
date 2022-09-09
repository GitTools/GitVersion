using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.VersionCalculation;

public class VersionStrategyModule : GitVersionModule
{
    public override void RegisterTypes(IServiceCollection services)
    {
        var versionStrategies = FindAllDerivedTypes<IVersionStrategy>(Assembly.GetAssembly(GetType()));

        foreach (var versionStrategy in versionStrategies.Where(x => !x.IsAbstract && !x.IsInterface))
        {
            services.AddSingleton(typeof(IVersionStrategy), versionStrategy);
        }
    }
}
