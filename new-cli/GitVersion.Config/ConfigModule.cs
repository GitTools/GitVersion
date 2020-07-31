using GitVersion.Config.Init;
using GitVersion.Config.Show;
using GitVersion.Infrastructure;

namespace GitVersion.Config
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