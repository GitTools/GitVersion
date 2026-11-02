using GitVersion.FileSystemGlobbing;
using GitVersion.Telemetry;

namespace GitVersion;

/// <summary>Registers CLI services and the allowlisted invocation projection used by telemetry.</summary>
internal class GitVersionAppModule(string[]? args = null, bool useLegacyParser = false) : IGitVersionModule
{
    /// <summary>Registers argument parsing, execution, and the deferred telemetry argument projection.</summary>
    public void RegisterTypes(IServiceCollection services)
    {
        if (useLegacyParser)
        {
            services.AddSingleton<IArgumentParser, LegacyArgumentParser>();
            services.AddSingleton<IHelpWriter, HelpWriter>();
            services.AddSingleton<IVersionWriter, VersionWriter>();
        }
        else
        {
            services.AddSingleton<IArgumentParser, ArgumentParser>();
            services.AddSingleton<CliSchemaExporter>();
        }

        services.AddSingleton<IGlobbingResolver, GlobbingResolver>();
        services.AddSingleton<IGitVersionExecutor, GitVersionExecutor>();
        services.AddSingleton(sp => new Lazy<IGitVersionExecutor>(sp.GetRequiredService<IGitVersionExecutor>));
        services.AddSingleton<ConfigurationQueryExecutor>();
        services.AddSingleton<ConfigurationValidationExecutor>();
        services.AddSingleton<GitVersionToolRunner>();
        services.AddSingleton<IConfigurationMigrationExecutor, ConfigurationMigrationExecutor>();
        services.AddSingleton<GitVersionApp>();
        services.AddSingleton(sp => TelemetryInvocationFactory.Create(sp.GetRequiredService<Arguments>()));

        services.AddSingleton(sp => sp.GetRequiredService<IArgumentParser>().ParseArguments(args ?? []));
        services.AddSingleton(sp =>
        {
            var arguments = sp.GetRequiredService<Arguments>();
            var gitVersionOptions = arguments.ToOptions();
            return Options.Create(gitVersionOptions);
        });
    }
}
