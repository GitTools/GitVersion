using GitVersion.Configuration.Init;
using GitVersion.Extensions;
using GitVersion.VersionCalculation.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Configuration;

public class GitVersionConfigurationModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(new GitVersionInitModule());

        services.AddSingleton<IGitVersionCacheKeyFactory, GitVersionCacheKeyFactory>();
        services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
        services.AddSingleton<IConfigurationFileLocator, ConfigurationFileLocator>();
    }
}
