using GitVersion.Infrastructure;
using GitVersion.Init;
using GitVersion.Show;

namespace GitVersion;

public class ConfigModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        services.AddSingleton<ICommand, ConfigCommand>();
        services.AddSingleton<ICommand, ConfigInitCommand>();
        services.AddSingleton<ICommand, ConfigShowCommand>();
    }
}