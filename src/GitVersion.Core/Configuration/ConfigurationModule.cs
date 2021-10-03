using GitVersion.Configuration.Init;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Configuration;

public class ConfigurationModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(new GitVersionInitModule());

        services.AddSingleton<IConfigProvider, ConfigProvider>();
        services.AddSingleton<IConfigFileLocator, ConfigFileLocator>();
        services.AddSingleton<IBranchConfigurationCalculator, BranchConfigurationCalculator>();
    }
}
