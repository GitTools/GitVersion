using Microsoft.Extensions.DependencyInjection;

namespace GitVersion;

public class GitVersionLibGit2SharpModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<GitRepository>();
        services.AddSingleton<IIgnoredFilterProvider, IgnoredFilterProvider>();
        services.AddSingleton<IGitRepository, IgnoredFilteringGitRepositoryDecorator>();
        services.AddSingleton<IMutatingGitRepository, IgnoredFilteringGitRepositoryDecorator>();

        services.AddSingleton<IGitRepositoryInfo, GitRepositoryInfo>();
    }
}
