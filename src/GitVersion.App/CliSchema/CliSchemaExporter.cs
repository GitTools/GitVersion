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
        var command = ExportCommand(root, values) with
        {
            Name = "gitversion",
            Metadata = [new("gitversion.optionArgumentSeparators",
                JsonSerializer.SerializeToElement(new[] { " ", "=", ":" }, CliSchemaJsonContext.Default.StringArray))]
        };
        var document = new CliSchemaDocument("0.1", new("GitVersion", version), command, new());
        return JsonSerializer.Serialize(document, CliSchemaJsonContext.Default.CliSchemaDocument);
    }

    private static CliSchemaCommand ExportCommand(Command item, IReadOnlyDictionary<Symbol, CliSchemaValues>? values) => new(
        item.Name, Aliases(item.Name, item.Aliases), item.Description, item.Hidden,
        [.. item.Arguments.Select(argument => ExportArgument(argument, argument.Name,
            argument.Arity, argument.ValueType, argument.HasDefaultValue, values))],
        [.. item.Options.Select(option => ExportOption(option, values))],
        [.. item.Subcommands.Select(command => ExportCommand(command, values))]);

    private static CliSchemaOption ExportOption(Option option, IReadOnlyDictionary<Symbol, CliSchemaValues>? values) => new(
        option.Name, Aliases(option.Name, option.Aliases), option.Description, option.Hidden,
        option.Required, option.Recursive,
        option.Arity.MaximumNumberOfValues == 0 ? [] :
            [ExportArgument(option, option.HelpName ?? option.Name.TrimStart('-'),
                option.Arity, option.ValueType, false, values)],
        [BooleanMetadata("gitversion.allowMultipleArgumentsPerToken", option.AllowMultipleArgumentsPerToken)]);

    private static CliSchemaArgument ExportArgument(Symbol symbol, string name, ArgumentArity arity, Type type,
        bool hasDefault, IReadOnlyDictionary<Symbol, CliSchemaValues>? values)
    {
        var valueType = type.IsArray ? type.GetElementType()! : Nullable.GetUnderlyingType(type) ?? type;
        var choices = GetChoices(symbol, valueType, values);
        var positional = symbol is Argument;
        // System.CommandLine represents unbounded arity with the ZeroOrMore sentinel.
        var maximum = arity.MaximumNumberOfValues == ArgumentArity.ZeroOrMore.MaximumNumberOfValues
            ? (int?)null : arity.MaximumNumberOfValues;
        return new(name, positional ? symbol.Description : null, positional && symbol.Hidden,
            arity.MinimumNumberOfValues > 0 && !hasDefault, new(arity.MinimumNumberOfValues, maximum),
            choices?.AcceptedValues, GetChoiceMetadata(choices));
    }

    private static CliSchemaValues? GetChoices(Symbol symbol, Type valueType, IReadOnlyDictionary<Symbol, CliSchemaValues>? values)
    {
        if (values is not null && values.TryGetValue(symbol, out var supplement))
        {
            return supplement;
        }
        if (valueType.IsEnum)
        {
            return new(Enum.GetNames(valueType), true, true);
        }
        return valueType == typeof(bool) ? new(["true", "false"], true) : null;
    }

    private static CliSchemaMetadata[] GetChoiceMetadata(CliSchemaValues? choices)
    {
        if (choices is null)
        {
            return [];
        }
        List<CliSchemaMetadata> metadata =
        [
            BooleanMetadata("gitversion.caseInsensitiveValues", choices.CaseInsensitive),
            BooleanMetadata("gitversion.acceptedValuesExhaustive", !choices.Numeric)
        ];
        if (choices.Numeric)
        {
            metadata.Add(BooleanMetadata("gitversion.acceptsNumericValues", true));
        }
        return [.. metadata];
    }

    private static string[] Aliases(string name, IEnumerable<string> aliases) =>
        [.. aliases.Where(alias => alias != name).Order(StringComparer.Ordinal)];

    private static CliSchemaMetadata BooleanMetadata(string name, bool value) =>
        new(name, JsonSerializer.SerializeToElement(value, CliSchemaJsonContext.Default.Boolean));
}
