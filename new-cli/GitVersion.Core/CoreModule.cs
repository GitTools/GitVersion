using GitVersion.Infrastructure;

namespace GitVersion
{
    public class CoreModule : IGitVersionModule
    {
        public void RegisterTypes(IContainerRegistrar services)
        {
            services.AddSingleton<IService, Service>();
        }
    }
}