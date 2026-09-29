using System.CommandLine;
using System.Reflection;
using System.Text.Json;

namespace GitVersion;

internal sealed class CliSchemaExporter
{
    public string Export(RootCommand root, IReadOnlyDictionary<Symbol, CliSchemaValues>? values = null)
    {
        var assembly = typeof(ArgumentParser).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? assembly.GetName().Version?.ToString() ?? "0.0.0";
        var command = ExportCommand(root) with
        {
            Name = "gitversion",
            Metadata = [new("gitversion.optionArgumentSeparators",
                JsonSerializer.SerializeToElement(new[] { " ", "=", ":" }, CliSchemaJsonContext.Default.StringArray))]
        };
        var document = new CliSchemaDocument("0.1", new("GitVersion", version), command, new());
        return JsonSerializer.Serialize(document, CliSchemaJsonContext.Default.CliSchemaDocument);

        CliSchemaCommand ExportCommand(Command item) => new(
            item.Name, Aliases(item.Name, item.Aliases), item.Description, item.Hidden,
            [.. item.Arguments.Select(argument => ExportArgument(argument, argument.Name, argument.Description,
                argument.Hidden, argument.Arity, argument.ValueType, argument.HasDefaultValue))],
            [.. item.Options.Select(ExportOption)], [.. item.Subcommands.Select(ExportCommand)]);

        CliSchemaOption ExportOption(Option option) => new(
            option.Name, Aliases(option.Name, option.Aliases), option.Description, option.Hidden,
            option.Required, option.Recursive,
            option.Arity.MaximumNumberOfValues == 0 ? [] :
                [ExportArgument(option, option.HelpName ?? option.Name.TrimStart('-'), null, false,
                    option.Arity, option.ValueType, false)],
            [BooleanMetadata("gitversion.allowMultipleArgumentsPerToken", option.AllowMultipleArgumentsPerToken)]);

        CliSchemaArgument ExportArgument(Symbol symbol, string name, string? description, bool hidden,
            ArgumentArity arity, Type type, bool hasDefault)
        {
            var valueType = type.IsArray ? type.GetElementType()! : Nullable.GetUnderlyingType(type) ?? type;
            var choices = values is not null && values.TryGetValue(symbol, out var supplement)
                ? supplement
                : valueType.IsEnum ? new CliSchemaValues(Enum.GetNames(valueType), true, true)
                : valueType == typeof(bool) ? new CliSchemaValues(["true", "false"], true)
                : null;
            List<CliSchemaMetadata> metadata = [];
            if (choices is not null)
            {
                metadata.Add(BooleanMetadata("gitversion.caseInsensitiveValues", choices.CaseInsensitive));
                metadata.Add(BooleanMetadata("gitversion.acceptedValuesExhaustive", !choices.Numeric));
                if (choices.Numeric)
                {
                    metadata.Add(BooleanMetadata("gitversion.acceptsNumericValues", true));
                }
            }
            // System.CommandLine represents unbounded arity with the ZeroOrMore sentinel.
            var maximum = arity.MaximumNumberOfValues == ArgumentArity.ZeroOrMore.MaximumNumberOfValues
                ? (int?)null : arity.MaximumNumberOfValues;
            return new(name, description, hidden, arity.MinimumNumberOfValues > 0 && !hasDefault,
                new(arity.MinimumNumberOfValues, maximum), choices?.AcceptedValues, [.. metadata]);
        }
    }

    private static string[] Aliases(string name, IEnumerable<string> aliases) =>
        [.. aliases.Where(alias => alias != name).Order(StringComparer.Ordinal)];

    private static CliSchemaMetadata BooleanMetadata(string name, bool value) =>
        new(name, JsonSerializer.SerializeToElement(value, CliSchemaJsonContext.Default.Boolean));
}
