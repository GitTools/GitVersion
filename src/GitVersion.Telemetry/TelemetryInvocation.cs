namespace GitVersion.Telemetry;

/// <summary>Contains CLI eligibility flags and deferred projections of option names and the tool release.</summary>
/// <remarks>Raw arguments and their values stay in the CLI assembly.</remarks>
internal sealed record TelemetryInvocation(bool OptOut, bool Excluded, Func<string[]> Options, Func<string?> ToolVersion);
