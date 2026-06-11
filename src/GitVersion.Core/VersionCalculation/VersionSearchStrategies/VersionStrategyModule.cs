namespace GitVersion.VersionCalculation;

/// <summary>Automatically discovers and registers all concrete <see cref="IVersionStrategy"/> implementations found in the current assembly.</summary>
public class VersionStrategyModule : IGitVersionModule
{
    /// <summary>Scans the assembly for <see cref="IVersionStrategy"/> implementations and registers each as a singleton.</summary>
    public void RegisterTypes(IServiceCollection services)
    {
        var versionStrategies = IGitVersionModule.FindAllDerivedTypes<IVersionStrategy>(Assembly.GetAssembly(GetType()))
            .Where(x => x is { IsAbstract: false, IsInterface: false });

        foreach (var versionStrategy in versionStrategies)
        {
            services.AddSingleton(typeof(IVersionStrategy), versionStrategy);
        }
    }
}
