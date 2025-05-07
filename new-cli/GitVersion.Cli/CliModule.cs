using GitVersion.Infrastructure;
using GitVersion.SystemCommandline;

namespace GitVersion;

public class CliModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services)
        => services
            .AddSingleton<IGitVersionAppRunner, GitVersionAppRunner>()
            .AddLogging();
}
