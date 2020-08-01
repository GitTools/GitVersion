using GitVersion.Configuration.Init;
using GitVersion.Configuration.Show;
using GitVersion.Infrastructure;

namespace GitVersion.Configuration
{
    public class ConfigModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<IRootCommandHandler, ConfigCommandHandler>();
            services.AddSingleton<IConfigCommandHandler, ConfigInitCommandHandler>();
            services.AddSingleton<IConfigCommandHandler, ConfigShowCommandHandler>();
        }
    }
}