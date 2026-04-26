using System.Globalization;

namespace GitVersion;

internal enum TelemetryValueKind
{
    Plain,
    Path,
    PathOrBoolean,
    Sensitive
}

internal static class TelemetryArgumentNames
{
    public const string AllowShallow = "allow-shallow";
    public const string Branch = "branch";
    public const string Commit = "commit";
    public const string Config = "config";
    public const string Diagnose = "diagnose";
    public const string DynamicRepoLocation = "dynamic-repo-location";
    public const string EnsureAssemblyInfo = "ensure-assembly-info";
    public const string Format = "format";
    public const string LogFile = "log-file";
    public const string NoCache = "no-cache";
    public const string NoFetch = "no-fetch";
    public const string NoNormalize = "no-normalize";
    public const string Output = "output";
    public const string OutputFile = "output-file";
    public const string OverrideConfig = "override-config";
    public const string Password = "password";
    public const string Path = "path";
    public const string ShowConfig = "show-config";
    public const string ShowVariable = "show-variable";
    public const string TargetPath = "target-path";
    public const string TelemetryOptOut = "telemetry-opt-out";
    public const string UpdateAssemblyInfo = "update-assembly-info";
    public const string UpdateProjectFiles = "update-project-files";
    public const string UpdateWixVersionFile = "update-wix-version-file";
    public const string Url = "url";
    public const string Username = "username";
    public const string Verbosity = "verbosity";
}

internal static class TelemetryReleaseDate
{
    public const string MetadataKey = "GitVersionReleaseDate";
    public const string Format = "yyyy-MM-dd";

    public static bool TryGetReleaseDate(Assembly assembly, out DateOnly releaseDate)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (assembly
            .GetCustomAttributes(typeof(AssemblyMetadataAttribute), false)
            .OfType<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == MetadataKey) is not { Value: { } value })
        {
            releaseDate = default;
            return false;
        }

        return DateOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);
    }

    public static bool IsWithinWindow(DateOnly releaseDate, DateOnly utcToday) =>
        utcToday < releaseDate.AddMonths(3);
}

internal sealed record TelemetryArgument(string Name, IReadOnlyList<string> Values);

internal sealed record CommandLineTelemetry(
    string ToolVersion,
    string ParserImplementation,
    string Command,
    string? Subcommand,
    IReadOnlyList<TelemetryArgument> Arguments
);

internal sealed class TelemetryCollectionBuilder(string parserImplementation)
{
    private const string CommandName = "gitversion";
    private const string PathRedactedValue = "<redacted:path>";
    private const string SensitiveRedactedValue = "<redacted:sensitive>";

    private readonly List<TelemetryArgument> arguments = [];

    public void AddFlag(string name) => AddValues(name, ["true"]);

    public void AddValue(string name, string? value, TelemetryValueKind kind = TelemetryValueKind.Plain)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        AddValues(name, [value], kind);
    }

    public void AddValues(string name, IEnumerable<string>? values, TelemetryValueKind kind = TelemetryValueKind.Plain)
    {
        if (values == null)
        {
            return;
        }

        var sanitizedValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Sanitize(value, kind))
            .ToArray();

        if (sanitizedValues.Length == 0)
        {
            return;
        }

        this.arguments.Add(new TelemetryArgument(name, sanitizedValues));
    }

    public CommandLineTelemetry Build() => new(
        ToolVersion: TelemetryVersionProvider.GetCurrentVersion(),
        ParserImplementation: parserImplementation,
        Command: CommandName,
        Subcommand: null,
        Arguments: this.arguments
    );

    private static string Sanitize(string value, TelemetryValueKind kind) => kind switch
    {
        TelemetryValueKind.Path => PathRedactedValue,
        TelemetryValueKind.PathOrBoolean when value.IsTrue() || value.IsFalse() => value.ToLowerInvariant(),
        TelemetryValueKind.PathOrBoolean => PathRedactedValue,
        TelemetryValueKind.Sensitive => SensitiveRedactedValue,
        _ => value
    };
}

internal static class TelemetryVersionProvider
{
    public static string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        if (assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .FirstOrDefault() is AssemblyInformationalVersionAttribute attribute)
        {
            return attribute.InformationalVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "unknown";
    }
}
