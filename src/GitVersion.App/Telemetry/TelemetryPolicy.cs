namespace GitVersion;

internal static class TelemetryPolicy
{
    public const string DoNotTrackEnvironmentVariable = "DO_NOT_TRACK";
    public const string GitVersionTelemetryOptOutEnvironmentVariable = "GITVERSION_TELEMETRY_OPTOUT";

    public static bool IsOptedOut(IEnvironment environment, bool telemetryOptOut) =>
        telemetryOptOut
        || environment.GetEnvironmentVariable(DoNotTrackEnvironmentVariable).IsTrue()
        || environment.GetEnvironmentVariable(GitVersionTelemetryOptOutEnvironmentVariable).IsTrue();
}
