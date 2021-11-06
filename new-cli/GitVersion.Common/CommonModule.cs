using GitVersion.Infrastructure;

namespace GitVersion;

public class CommonModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        services.AddSingleton<IService, Service>();
    }
}