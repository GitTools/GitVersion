using GitVersion.Git;

namespace GitVersion;

public class GitVersionLibGit2SharpModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IGitCliExecutor, GitCliExecutor>();
        services.AddSingleton<IGitCliMutator, GitCliMutator>();
        services.AddSingleton<IGitRepository, GitRepository>();
        services.AddSingleton<IMutatingGitRepository>(sp => (IMutatingGitRepository)sp.GetRequiredService<IGitRepository>());
        services.AddSingleton<IGitRepositoryInfo, GitRepositoryInfo>();
    }
}
