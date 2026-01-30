using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Output;

namespace GitVersion;

internal static class CliHost
{
    internal static HostApplicationBuilder CreateCliHostBuilder(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        RegisterGitVersionModules(builder.Services, args);

        return builder;
    }

    private static void RegisterGitVersionModules(IServiceCollection services, string[] args)
    {
        services.AddModule(new GitVersionCoreModule());
        services.AddModule(new GitVersionBuildAgentsModule());
        services.AddModule(new GitVersionConfigurationModule());
        services.AddModule(new GitVersionOutputModule());

        services.AddModule(new GitVersionLibGit2SharpModule());
        services.AddModule(new GitVersionAppModule(args));
    }
}
