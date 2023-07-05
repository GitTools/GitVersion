using GitVersion.Infrastructure;

namespace GitVersion;

public class LibGit2SharpCoreModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services) => services.AddSingleton<IGitRepository, GitRepository>();
}
