using GitVersion.Extensions;
using GitVersion.Generated;
using GitVersion.Infrastructure;

namespace GitVersion.SystemCommandline;

public class CliModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
    {
        services.RegisterModule(new CommandsModule());
        services.AddSingleton<IGitVersionAppRunner, GitVersionAppRunner>();
    }
}
