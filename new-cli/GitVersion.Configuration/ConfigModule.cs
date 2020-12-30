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
            services.AddSingleton<ICommandHandler, ConfigCommandHandler>();
            services.AddSingleton<ICommandHandler, ConfigInitCommandHandler>();
            services.AddSingleton<ICommandHandler, ConfigShowCommandHandler>();
        }
    }
}