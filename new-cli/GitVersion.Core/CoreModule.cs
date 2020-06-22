using GitVersion.Core.Infrastructure;

namespace GitVersion.Core
{
    public class CoreModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<IService, Service>();
        }
    }
}