using GitVersion.Agents;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Output;
using Serilog;
using Serilog.Core;

namespace GitVersion;

internal static class CliHost
{
    internal static HostApplicationBuilder CreateCliHostBuilder(string[] args)
    {
        var bootstrapSwitch = new LoggingLevelSwitch();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(bootstrapSwitch)
            .WriteTo.Console()
            .CreateBootstrapLogger();

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton(bootstrapSwitch);

        RegisterGitVersionModules(builder.Services, args);

        return builder;
    }

    private static void RegisterGitVersionModules(IServiceCollection services, string[] args)
    {
        var selections = FeatureSelections.Resolve();
        services.AddSingleton(selections);
        services.AddModule(new GitVersionCoreModule());
        services.AddModule(new GitVersionBuildAgentsModule());
        services.AddModule(new GitVersionConfigurationModule());
        services.AddModule(new GitVersionOutputModule());

        services.AddModule(selections.GitBackend == GitBackend.Managed
            ? new GitVersionManagedGitModule()
            : new GitVersionLibGit2SharpModule());

        services.AddModule(new GitVersionAppModule(args, selections.ArgumentParser == ArgumentParserVersion.V6));
    }
}
