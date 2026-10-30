using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.FileSystemGlobbing;
using GitVersion.Telemetry;

namespace GitVersion;

/// <summary>Registers CLI services, keeping telemetry disabled for the legacy parser and deferred for the POSIX parser.</summary>
internal class GitVersionAppModule(string[]? args = null, bool useLegacyParser = false) : IGitVersionModule
{
    /// <summary>Registers argument parsing, execution, and policy-controlled telemetry with stderr disclosure.</summary>
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
        services.AddSingleton(TelemetryCampaign.Current);
        if (useLegacyParser)
        {
            services.AddSingleton<ITelemetryReporter, DisabledTelemetryReporter>();
        }
        else
        {
            services.AddSingleton<TelemetryRuntimeContextReader>();
            services.AddSingleton(sp => new TelemetryNotice(sp.GetRequiredService<IFileSystem>(),
                () => SysEnv.GetFolderPath(SysEnv.SpecialFolder.LocalApplicationData), Console.Error.WriteLine));
            services.AddSingleton<ITelemetryReporter>(sp => new TelemetryReporter(
                sp.GetRequiredService<Arguments>(), sp.GetRequiredService<IEnvironment>(),
                sp.GetRequiredService<TelemetryCampaign>(), TimeProvider.System,
                () => sp.GetRequiredService<ICurrentBuildAgent>(), Random.Shared.NextDouble,
                TelemetryReporter.CreateClient, sp.GetRequiredService<TelemetryNotice>().Show,
                () => sp.GetRequiredService<TelemetryRuntimeContextReader>().Read()));
        }

        services.AddSingleton(sp => sp.GetRequiredService<IArgumentParser>().ParseArguments(args ?? []));
        services.AddSingleton(sp =>
        {
            var arguments = sp.GetRequiredService<Arguments>();
            var gitVersionOptions = arguments.ToOptions();
            return Options.Create(gitVersionOptions);
        });
    }
}
