using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Git;

public class ManagedCoreModule : IGitVersionModule
{
    public IServiceCollection RegisterTypes(IServiceCollection services)
    {
        GitBackendSelector.Resolve();
        services.AddSingleton<IGitCliExecutor, GitCliExecutor>();
        services.AddSingleton<IGitCliMutator, GitCliMutator>();
        return services.AddSingleton<IGitRepository, ManagedGitRepository>();
    }
}
