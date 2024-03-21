using GitVersion.Infrastructure;

namespace GitVersion.Git;

public class LibGit2SharpCoreModule : IGitVersionModule
{
    public void RegisterTypes(IContainerRegistrar services) => services.AddSingleton<IGitRepository, GitRepository>();
}
