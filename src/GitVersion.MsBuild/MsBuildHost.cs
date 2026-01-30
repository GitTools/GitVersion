using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Output;

namespace GitVersion.MsBuild;

internal static class MsBuildHost
{
    internal static void RegisterGitVersionModules(IServiceCollection services, GitVersionTaskBase task)
    {
        services.AddModule(new GitVersionCoreModule());
        services.AddModule(new GitVersionBuildAgentsModule());
        services.AddModule(new GitVersionConfigurationModule());
        services.AddModule(new GitVersionOutputModule());

        services.AddModule(new GitVersionMsBuildModule(task));
    }
}
