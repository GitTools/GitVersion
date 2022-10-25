using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.VersionCalculation;

public class VersionStrategyModule : GitVersionModule
{
    public override void RegisterTypes(IServiceCollection services)
    {
        var versionStrategies = FindAllDerivedTypes<IVersionStrategy>(Assembly.GetAssembly(GetType()))
            .Where(x => !x.IsAbstract && !x.IsInterface);

        foreach (var versionStrategy in versionStrategies)
        {
            services.AddSingleton(typeof(IVersionStrategy), versionStrategy);
        }
    }
}
