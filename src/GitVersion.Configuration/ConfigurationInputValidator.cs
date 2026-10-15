using System.Globalization;

namespace GitVersion.Configuration;

// Validate the supplied leaves before defaults can hide misspelled or ill-typed input.
// Names and types come from the runtime configuration model, not a second schema.
internal static class ConfigurationInputValidator
{
    public static void Validate(IReadOnlyDictionary<object, object?> document, ConfigurationVersion version)
        => ValidateObject(document, typeof(GitVersionConfiguration), "", version);

    private static void ValidateObject(IReadOnlyDictionary<object, object?> document, Type type, string prefix, ConfigurationVersion version)
    {
        var properties = type.GetProperties().Where(p => p.GetCustomAttribute<JsonPropertyNameAttribute>() is not null)
            .ToDictionary(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name);
        foreach (var (key, value) in document)
        {
            var name = key as string ?? "<key>";
            var path = prefix + name;
            if (prefix.Length == 0 && name != "workflow" && version == ConfigurationVersion.V7)
            {
                path = (ConfigurationDocumentMapper.IsOutputProperty(name) ? "output." : "calculation.") + name;
            }

            if (!properties.TryGetValue(name, out var property))
            {
                throw Error(path, "Unknown configuration property.");
            }

            if (version == ConfigurationVersion.V7 && prefix.StartsWith("calculation.branches.", StringComparison.Ordinal)
                && ConfigurationDocumentMapper.IsOutputBranchProperty(name))
            {
                path = "output." + path["calculation.".Length..];
            }
            if (name == "workflow" && value is not null and not string)
            {
                throw Error(path, "Expected a workflow name.");
            }
            if (name == "commits-before" && value is not null
                && !DateTimeOffset.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                throw Error(path, "Expected a valid commit cutoff date.");
            }

            ValidateValue(value, property.PropertyType, path, version);
        }
    }

    private static void ValidateValue(object? value, Type type, string path, ConfigurationVersion version)
    {
        if (value is null)
        {
            // Nullable scalar settings explicitly support null; collections/objects must remain usable.
            if (type == typeof(string) || Nullable.GetUnderlyingType(type) is not null)
            {
                return;
            }

            throw Error(path, "Expected a non-null configuration value.");
        }
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(GitVersion.VersionCalculation.VersionStrategies[]))
        {
            ValidateStrategies(value, path);
            return;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            ValidateDictionary(value, type.GenericTypeArguments[1], path, version);
            return;
        }
        if (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
        {
            var elementType = type.IsArray ? type.GetElementType()! : type.GenericTypeArguments[0];
            ValidateSequence(value, elementType, path, version);
            return;
        }

        ValidateLeaf(value, type, path, version);
    }

    private static void ValidateStrategies(object value, string path)
    {
        try
        {
            var strategies = new ConfigurationSerializer().Deserialize<GitVersion.VersionCalculation.VersionStrategies[]>(ConfigurationSerializer.SerializeLegacy(value));
            if (strategies.Any(strategy => !Enum.IsDefined(strategy)))
            {
                throw Error(path, "Expected supported version strategies.");
            }
        }
        catch (Exception exception) when (exception is SharpYaml.YamlException or ArgumentException)
        {
            throw Error(path, "Expected supported version strategies.");
        }
    }

    private static void ValidateDictionary(object value, Type elementType, string path, ConfigurationVersion version)
    {
        if (value is not IReadOnlyDictionary<object, object?> map)
        {
            throw Error(path, "Expected a mapping.");
        }

        foreach (var (key, item) in map)
        {
            if (item is null)
            {
                throw Error(path + "." + key, "Expected a non-null mapping value.");
            }
            ValidateValue(item, elementType, path + "." + key, version);
        }
    }

    private static void ValidateSequence(object value, Type elementType, string path, ConfigurationVersion version)
    {
        if (value is not IEnumerable sequence || value is string or IDictionary or IReadOnlyDictionary<object, object?>)
        {
            throw Error(path, "Expected a sequence.");
        }

        var list = sequence.Cast<object?>().ToArray();
        for (var index = 0; index < list.Length; index++)
        {
            if (list[index] is null)
            {
                throw Error($"{path}[{index}]", "Expected a non-null sequence item.");
            }
            ValidateValue(list[index], elementType, $"{path}[{index}]", version);
        }
    }

    private static void ValidateLeaf(object value, Type type, string path, ConfigurationVersion version)
    {
        if (type == typeof(string))
        {
            if (value is not string && value is not ValueType)
            {
                throw Error(path, "Expected a scalar.");
            }
        }
        else if (type.IsEnum)
        {
            if (!IsSupportedEnum(value, type))
            {
                throw Error(path, "Expected a supported enum name.");
            }
        }
        else if (type == typeof(bool))
        {
            if (!bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out _))
            {
                throw Error(path, "Expected a boolean.");
            }
        }
        else if (type == typeof(int))
        {
            if (!int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                throw Error(path, "Expected a 32-bit integer.");
            }
        }
        else if (value is IReadOnlyDictionary<object, object?> nested)
        {
            ValidateObject(nested, type, path + ".", version);
        }
        else
        {
            throw Error(path, "Expected a mapping.");
        }
    }

    private static bool IsSupportedEnum(object value, Type type)
        => type.IsInstanceOfType(value) && Enum.IsDefined(type, value)
           || value is string text && Enum.GetNames(type).Contains(text, StringComparer.OrdinalIgnoreCase);

    private static ConfigurationValidationException Error(string path, string message)
        => new("GVCONFIG_PROPERTY", path, message);
}
