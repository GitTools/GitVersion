using GitVersion.Extensions;
using GitVersion.Generated;
using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.SystemCommandline;

public class CliModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.RegisterModule(new CommandsModule());
        services.AddSingleton<IGitVersionAppRunner, GitVersionAppRunner>();
    }
}
