using System.IO.Abstractions;
using GitVersion.Infrastructure;

namespace GitVersion;

public class CoreModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IEnvironment, Environment>();
        services.AddSingleton<IService, Service>();
    }
}
