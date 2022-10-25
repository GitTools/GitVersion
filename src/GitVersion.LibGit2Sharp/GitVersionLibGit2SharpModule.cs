using Microsoft.Extensions.DependencyInjection;

namespace GitVersion;

public class GitVersionLibGit2SharpModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IGitRepository, GitRepository>();
        services.AddSingleton<IMutatingGitRepository, GitRepository>();
        services.AddSingleton<IGitRepositoryInfo, GitRepositoryInfo>();
    }
}
