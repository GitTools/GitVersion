using GitVersion.Git;

namespace GitVersion;

/// <summary>
/// Registers the managed Git backend: all repository reads are served by the managed
/// object/reference readers, while mutating and network operations go through the git CLI.
/// </summary>
public class GitVersionManagedGitModule : IGitVersionModule
{
    /// <summary>Registers the services provided by this module into <paramref name="services"/>.</summary>
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IGitCliExecutor, GitCliExecutor>();
        services.AddSingleton<IGitCliMutator, GitCliMutator>();
        services.AddSingleton<IGitRepository, ManagedGitRepository>();
        services.AddSingleton<IMutatingGitRepository>(sp => (IMutatingGitRepository)sp.GetRequiredService<IGitRepository>());
        services.AddSingleton<IGitRepositoryInfo, ManagedGitRepositoryInfo>();
    }
}
