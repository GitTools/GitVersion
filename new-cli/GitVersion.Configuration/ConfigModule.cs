using GitVersion.Infrastructure;
using GitVersion.Init;
using GitVersion.Show;

namespace GitVersion;

public class ConfigModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        services.AddSingleton<ConfigCommand>();
        services.AddSingleton<ConfigInitCommand>();
        services.AddSingleton<ConfigShowCommand>();
    }
}
