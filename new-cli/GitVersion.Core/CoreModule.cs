using System.IO.Abstractions;
using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion;

public class CoreModule : IGitVersionModule
{
    public IServiceCollection RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IEnvironment, Environment>();
        services.AddSingleton<IService, Service>();
        return services;
    }
}
