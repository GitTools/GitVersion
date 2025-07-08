using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Git;

public class LibGit2SharpCoreModule : IGitVersionModule
{
    public IServiceCollection RegisterTypes(IServiceCollection services) => services.AddSingleton<IGitRepository, GitRepository>();
}
