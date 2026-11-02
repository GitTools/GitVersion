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

        services.AddSingleton<ITelemetryRuntimeContextReader, TelemetryRuntimeContextReader>();
        services.AddSingleton<ITelemetryNotice, TelemetryNotice>();
        services.AddSingleton<ITelemetryTransport, TelemetryTransport>();
        services.AddSingleton<ITelemetryReporter, TelemetryReporter>();
    }
}
