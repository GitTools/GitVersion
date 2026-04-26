namespace GitVersion;

internal class GitVersionTelemetryModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<ITelemetrySink, NoOpTelemetrySink>();
        services.AddSingleton<ITelemetryNoticeState, FileTelemetryNoticeState>();
        services.AddSingleton<ITelemetryReporter, TelemetryReporter>();
    }
}
