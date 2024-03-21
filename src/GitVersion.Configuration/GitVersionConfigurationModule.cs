using GitVersion.VersionCalculation.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Configuration;

public class GitVersionConfigurationModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IGitVersionCacheKeyFactory, GitVersionCacheKeyFactory>();
        services.AddSingleton<IConfigurationSerializer, ConfigurationSerializer>();
        services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
        services.AddSingleton<IConfigurationFileLocator, ConfigurationFileLocator>();
    }
}
