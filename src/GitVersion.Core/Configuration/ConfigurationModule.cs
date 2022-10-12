using GitVersion.Configurations.Init;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Configurations;

public class ConfigurationModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(new GitVersionInitModule());

        services.AddSingleton<IConfigProvider, ConfigProvider>();
        services.AddSingleton<IConfigFileLocator, ConfigFileLocator>();
    }
}
