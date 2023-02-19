using GitVersion.BuildAgents;
using GitVersion.Extensions;
using GitVersion.VersionConverters;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.MsBuild;

public class GitVersionMsBuildModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IGitVersionTaskExecutor, GitVersionTaskExecutor>();

        services.AddModule(new GitVersionCommonModule());
        services.AddModule(new GitVersionBuildAgentsModule());
        services.AddModule(new VersionConvertersModule());
    }
}
