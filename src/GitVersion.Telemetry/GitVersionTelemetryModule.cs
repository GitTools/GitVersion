using System.IO.Abstractions;
using GitVersion.Agents;

namespace GitVersion.Telemetry;

/// <summary>Registers policy-controlled telemetry, with collection excluded for the legacy parser.</summary>
/// <param name="useLegacyParser">Whether the host uses the unsupported legacy parser.</param>
public class GitVersionTelemetryModule(bool useLegacyParser = false) : IGitVersionModule
{
    /// <summary>Registers reporting and its deferred runtime metadata and disclosure dependencies.</summary>
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton(TelemetryCampaign.Current);
        if (useLegacyParser)
        {
            services.AddSingleton<ITelemetryReporter, DisabledTelemetryReporter>();
            return;
        }

        services.AddSingleton<TelemetryRuntimeContextReader>();
        services.AddSingleton(sp => new TelemetryNotice(sp.GetRequiredService<IFileSystem>(),
            () => SysEnv.GetFolderPath(SysEnv.SpecialFolder.LocalApplicationData), Console.Error.WriteLine));
        services.AddSingleton<ITelemetryReporter>(sp => new TelemetryReporter(
            sp.GetRequiredService<TelemetryInvocation>(), sp.GetRequiredService<IEnvironment>(),
            sp.GetRequiredService<TelemetryCampaign>(), TimeProvider.System,
            () => sp.GetRequiredService<ICurrentBuildAgent>(), Random.Shared.NextDouble,
            TelemetryReporter.CreateClient, sp.GetRequiredService<TelemetryNotice>().Show,
            () => sp.GetRequiredService<TelemetryRuntimeContextReader>().Read()));
    }
}
