using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GitVersion.Telemetry;

/// <summary>Defines a release-maintained campaign with a fixed ingestion region and capped sampling rates.</summary>
/// <remarks>
/// Cake embeds the release or prerelease build's UTC date. Activation requires a reviewed public project write key
/// and a no-card Free project; missing or invalid configuration prevents collection.
/// </remarks>
internal sealed record TelemetryCampaign(
    string? ProjectKey,
    string? Region,
    string? StartDate,
    double DirectProbability = 0.01,
    double AutomatedProbability = 0.0001)
{
    /// <summary>Gets the EU campaign with build-time public key and release-date metadata; defaults to disabled.</summary>
    internal static TelemetryCampaign Current { get; } = new(ReadProjectKey(typeof(TelemetryCampaign).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()), "eu",
        ReadStartDate(typeof(TelemetryCampaign).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()));

    /// <summary>Reads the public write key only when exactly one matching metadata entry exists.</summary>
    internal static string? ReadProjectKey(IEnumerable<AssemblyMetadataAttribute> metadata)
    {
        var values = metadata.Where(attribute => attribute.Key == "TelemetryProjectKey").Take(2).ToArray();
        return values.Length == 1 ? values[0].Value : null;
    }

    /// <summary>Reads the campaign date only when exactly one matching assembly metadata entry exists.</summary>
    /// <returns>The embedded value, or <see langword="null"/> when metadata is absent or ambiguous.</returns>
    internal static string? ReadStartDate(IEnumerable<AssemblyMetadataAttribute> metadata)
    {
        var dates = metadata.Where(attribute => attribute.Key == "TelemetryCampaignStart").Take(2).ToArray();
        // Ambiguous metadata must not select an arbitrary collection window.
        return dates.Length == 1 ? dates[0].Value : null;
    }

    /// <summary>Gets the allowlisted ingestion endpoint, or null for an unsupported region.</summary>
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded",
        Justification = "Only these reviewed PostHog ingestion endpoints are allowed; arbitrary destinations must be rejected.")]
    internal Uri? Endpoint => Region switch
    {
        "eu" => new Uri("https://eu.i.posthog.com/i/v0/e/"),
        "us" => new Uri("https://us.i.posthog.com/i/v0/e/"),
        _ => null
    };

    /// <summary>Validates configuration and checks the inclusive start and exclusive three-month UTC cutoff.</summary>
    /// <param name="today">The current UTC calendar date.</param>
    internal bool IsActive(DateOnly today) =>
        Endpoint != null &&
        ProjectKey is { Length: >= 5 and <= 200 } && ProjectKey.StartsWith("phc_", StringComparison.Ordinal) &&
        ProjectKey.All(c => char.IsAsciiLetterOrDigit(c) || c == '_') &&
        double.IsFinite(DirectProbability) && DirectProbability is >= 0 and <= 0.01 &&
        double.IsFinite(AutomatedProbability) && AutomatedProbability is >= 0 and <= 0.0001 &&
        DateOnly.TryParseExact(StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
        start.Year < 9999 && start <= today && today < start.AddMonths(3);
}
