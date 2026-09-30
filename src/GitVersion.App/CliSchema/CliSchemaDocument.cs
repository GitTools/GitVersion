using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitVersion;

internal sealed record CliSchemaDocument(string Opencli, CliSchemaInfo Info, CliSchemaCommand Command, CliSchemaConventions Conventions);
internal sealed record CliSchemaInfo(string Title, string Version);
internal sealed record CliSchemaConventions(bool GroupOptions = true, string OptionSeparator = " ");
internal sealed record CliSchemaCommand(
    string Name, string[] Aliases, string? Description, bool Hidden,
    CliSchemaArgument[] Arguments, CliSchemaOption[] Options, CliSchemaCommand[] Commands,
    CliSchemaMetadata[]? Metadata = null);
internal sealed record CliSchemaOption(
    string Name, string[] Aliases, string? Description, bool Hidden, bool Required, bool Recursive,
    CliSchemaArgument[] Arguments, CliSchemaMetadata[] Metadata);
internal sealed record CliSchemaArgument(
    string Name, string? Description, bool Hidden, bool Required, CliSchemaArity Arity,
    string[]? AcceptedValues, CliSchemaMetadata[] Metadata);
internal sealed record CliSchemaArity(int Minimum, int? Maximum);
internal sealed record CliSchemaMetadata(string Name, JsonElement Value);
internal sealed record CliSchemaValues(string[] AcceptedValues, bool CaseInsensitive = false, bool Numeric = false);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CliSchemaDocument))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string[]))]
internal partial class CliSchemaJsonContext : JsonSerializerContext;
