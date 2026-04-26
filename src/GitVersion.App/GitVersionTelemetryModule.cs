namespace GitVersion;

internal class GitVersionTelemetryModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<ITelemetryReleaseDateProvider, AssemblyTelemetryReleaseDateProvider>();
        services.AddSingleton<ITelemetryUtcDateProvider, TelemetryUtcDateProvider>();
        services.AddSingleton<ITelemetryContextEnricher, TelemetryContextEnricher>();
        services.AddSingleton<ITelemetrySink, NoOpTelemetrySink>();
        services.AddSingleton<ITelemetryNoticeState, FileTelemetryNoticeState>();
        services.AddSingleton<ITelemetryReporter, TelemetryReporter>();
    }
}
