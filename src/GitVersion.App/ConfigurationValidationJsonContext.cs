namespace GitVersion;

internal sealed record ConfigurationValidationResult(
    int SchemaVersion,
    bool Valid,
    string? ConfigurationVersion,
    string? File,
    List<ConfigurationValidationDiagnostic> Diagnostics);

internal sealed record ConfigurationValidationDiagnostic(string Code, string Severity, string Path, string Message, int? Line = null, int? Column = null);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ConfigurationValidationResult))]
internal partial class ConfigurationValidationJsonContext : JsonSerializerContext;
