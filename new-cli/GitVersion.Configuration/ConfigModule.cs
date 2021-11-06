using GitVersion.Command;
using GitVersion.Configuration.Init;
using GitVersion.Configuration.Show;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration
{
    public class ConfigModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<ICommand, ConfigCommand>();
            services.AddSingleton<ICommand, ConfigInitCommand>();
            services.AddSingleton<ICommand, ConfigShowCommand>();
        }
    }
}