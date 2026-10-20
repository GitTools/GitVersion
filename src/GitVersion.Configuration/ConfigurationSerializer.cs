using SharpYaml;

namespace GitVersion.Configuration;

internal class ConfigurationSerializer : IConfigurationSerializer
{
    private static readonly HashSet<string> PreserveKeyOrderFor =
    [
        "branches",
        "merge-message-formats"
    ];

    private static readonly ConfigurationYamlContext GeneratedContext = ConfigurationYamlContext.Default;

    private static readonly YamlSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = HyphenatedJsonNamingPolicy.Instance,
        DefaultIgnoreCondition = YamlIgnoreCondition.WhenWritingNull,
        Converters = [VersionStrategiesConverter.Instance]
    };

    public T Deserialize<T>(string input) // NOSONAR: IConfigurationSerializer requires an instance method.
    {
        if (typeof(T) == typeof(Dictionary<object, object?>))
        {
            var graph = YamlSerializer.Deserialize<Dictionary<string, object?>>(input, SerializerOptions);
            return (T)(object)ConvertToObjectDictionary(graph);
        }

        if (typeof(T) == typeof(GitVersionConfiguration))
        {
            return (T)(object)DeserializeConfiguration(input, ConfigurationVersionSelector.Resolve(), "configuration document")!;
        }

        if (typeof(T) == typeof(GitVersion.VersionCalculation.VersionStrategies[]))
        {
            return YamlSerializer.Deserialize<T>(input, GeneratedContext)!;
        }

        return YamlSerializer.Deserialize<T>(input, SerializerOptions)!;
    }

    public string Serialize(object graph) // NOSONAR: IConfigurationSerializer requires an instance method.
    {
        var yaml = SerializeLegacy(graph);
        var configuration = YamlSerializer.Deserialize<Dictionary<string, object?>>(yaml, SerializerOptions) ?? [];
        if (graph is IGitVersionConfiguration && ConfigurationVersionSelector.Resolve() == ConfigurationVersion.V7)
        {
            configuration = ConfigurationDocumentMapper.Nest(configuration);
        }

        return YamlSerializer.Serialize(OrderProperties(configuration), SerializerOptions);
    }

    internal static string SerializeDocument(IReadOnlyDictionary<object, object?> document)
    {
        var yaml = SerializeLegacy(document);
        var configuration = YamlSerializer.Deserialize<Dictionary<string, object?>>(yaml, SerializerOptions) ?? [];
        return YamlSerializer.Serialize(OrderProperties(configuration), SerializerOptions);
    }

    public string SerializeProperty(IGitVersionConfiguration configuration, string path)
    {
        var value = ConfigurationPropertyQuery.GetValue(configuration, path);
        // Public configuration enums use their names in YAML. Avoid resolving a
        // runtime enum converter merely to display that same name as JSON.
        var scalar = value is Enum enumeration ? enumeration.ToString() : value;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            switch (scalar)
            {
                case null:
                    writer.WriteNullValue();
                    break;
                case string text:
                    writer.WriteStringValue(text);
                    break;
                case bool boolean:
                    writer.WriteBooleanValue(boolean);
                    break;
                case int integer:
                    writer.WriteNumberValue(integer);
                    break;
                case long integer:
                    writer.WriteNumberValue(integer);
                    break;
                default:
                    throw new ConfigurationException($"Unsupported scalar value for configuration property '{path}'.");
            }
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static IGitVersionConfiguration? ReadConfiguration(string input)
        => DeserializeConfiguration(input, ConfigurationVersionSelector.Resolve(), "configuration document");

    // Typed model contracts must use generated metadata; primitives and untyped
    // containers use SharpYaml's built-in converters. Keeping these paths
    // separate also avoids asking a model-only context for untyped graph metadata.
    internal static string SerializeLegacy(object graph) => graph switch
    {
        GitVersionConfiguration configuration => SerializeModel(configuration),
        BranchConfiguration branch => SerializeModel(branch),
        PreventIncrementConfiguration preventIncrement => SerializeModel(preventIncrement),
        IgnoreConfiguration ignore => SerializeModel(ignore),
        GitVersion.VersionCalculation.VersionStrategies[] strategies => YamlSerializer.Serialize(strategies, GeneratedContext),
        _ => YamlSerializer.Serialize(ConvertToYamlGraph(graph), SerializerOptions)
    };

    private static object? ConvertToYamlGraph(object? value) => value switch
    {
        IReadOnlyDictionary<object, object?> dictionary => ConvertToYamlDictionary(dictionary),
        IReadOnlyDictionary<string, object?> dictionary => dictionary.ToDictionary(item => item.Key, item => ConvertToYamlGraph(item.Value)),
        IList list => list.Cast<object?>().Select(ConvertToYamlGraph).ToList(),
        _ => value
    };

    private static Dictionary<string, object?> ConvertToYamlDictionary(IReadOnlyDictionary<object, object?> source)
    {
        // SharpYaml has reflection-free built-in contracts for string-keyed
        // untyped graphs. The configuration mapper uses object keys internally;
        // never ask NativeAOT to construct a dictionary converter for that type.
        Dictionary<string, object?> result = [];
        foreach (var (key, value) in source)
        {
            if (key is not string name)
            {
                throw new YamlException("Configuration mapping keys must be strings.");
            }
            result.Add(name, ConvertToYamlGraph(value));
        }
        return result;
    }

    private static string SerializeModel<T>(T model)
    {
        var yaml = YamlSerializer.Serialize(model, GeneratedContext);
        var document = YamlSerializer.Deserialize<Dictionary<string, object?>>(yaml, SerializerOptions) ?? [];
        // SharpYaml 3.16's generated writer applies WhenWritingNull to reference
        // properties but emits null Nullable<T> properties. Restore the model's
        // existing omission contract before merging defaults or displaying YAML.
        // This applies only to typed model properties; explicit nulls in input
        // documents and values in dynamic maps must be preserved.
        OmitNullModelProperties(document);
        return YamlSerializer.Serialize(document, SerializerOptions);
    }

    private static void OmitNullModelProperties(Dictionary<string, object?> model)
    {
        foreach (var (name, value) in model.ToArray())
        {
            if (value is null)
            {
                model.Remove(name);
            }
            else if (value is Dictionary<string, object?> nested)
            {
                if (name is "prevent-increment" or "ignore")
                {
                    OmitNullModelProperties(nested);
                }
                else if (name == "branches")
                {
                    foreach (var branch in nested.Values.OfType<Dictionary<string, object?>>())
                    {
                        OmitNullModelProperties(branch);
                    }
                }
            }
        }
    }

    internal static GitVersionConfiguration? DeserializeLegacyConfiguration(string input)
        => DeserializeConfiguration(input, ConfigurationVersion.V6, "internal effective configuration");

    private static GitVersionConfiguration? DeserializeConfiguration(
        string input,
        ConfigurationVersion version,
        string source)
    {
        try
        {
            var graph = YamlSerializer.Deserialize<Dictionary<string, object?>>(input, SerializerOptions);
            var objectGraph = ConvertToObjectDictionary(graph);
            var normalized = ConfigurationDocumentMapper.Normalize(objectGraph, version, source);
            var normalizedYaml = SerializeLegacy(normalized);
            return YamlSerializer.Deserialize<GitVersionConfiguration>(normalizedYaml, GeneratedContext);
        }
        catch (Exception exception) when (exception is not YamlException and not ConfigurationException)
        {
            throw new YamlException(exception.Message, exception);
        }
    }

    private static Dictionary<object, object?> ConvertToObjectDictionary(IReadOnlyDictionary<string, object?>? source)
    {
        if (source is null)
        {
            return [];
        }

        Dictionary<object, object?> result = [];
        foreach (var item in source)
        {
            result[item.Key] = ConvertValue(item.Value);
        }

        return result;
    }

    private static object? ConvertValue(object? value) => value switch
    {
        IReadOnlyDictionary<string, object?> dictionary => ConvertToObjectDictionary(dictionary),
        IDictionary<string, object?> dictionary => ConvertToObjectDictionary(new Dictionary<string, object?>(dictionary)),
        IList list => ConvertList(list),
        _ => value
    };

    private static List<object?> ConvertList(IList list)
    {
        List<object?> result = [];
        foreach (var item in list)
        {
            result.Add(ConvertValue(item));
        }

        return result;
    }

    private static Dictionary<string, object?> OrderProperties(
        IReadOnlyDictionary<string, object?> source,
        bool orderKeys = true)
    {
        IEnumerable<KeyValuePair<string, object?>> entries = orderKeys
            ? source.OrderBy(element => element.Key, StringComparer.Ordinal)
            : source;
        Dictionary<string, object?> result = [];

        foreach (var (key, value) in entries)
        {
            result[key] = OrderValue(value, orderKeys: !PreserveKeyOrderFor.Contains(key));
        }

        return result;
    }

    private static object? OrderValue(object? value, bool orderKeys) => value switch
    {
        IReadOnlyDictionary<string, object?> dictionary => OrderProperties(dictionary, orderKeys),
        IDictionary<string, object?> dictionary => OrderProperties(new Dictionary<string, object?>(dictionary), orderKeys),
        IList list => list.Cast<object?>().Select(item => OrderValue(item, orderKeys: true)).ToList(),
        _ => value
    };

    private sealed class HyphenatedJsonNamingPolicy : JsonNamingPolicy
    {
        public static JsonNamingPolicy Instance { get; } = new HyphenatedJsonNamingPolicy();

        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var builder = new StringBuilder(name.Length + 8);
            for (var index = 0; index < name.Length; index++)
            {
                var current = name[index];
                if (char.IsUpper(current))
                {
                    var hasPrevious = index > 0;
                    var hasNext = index + 1 < name.Length;
                    var previousIsLowerOrDigit = hasPrevious && (char.IsLower(name[index - 1]) || char.IsDigit(name[index - 1]));
                    var nextIsLower = hasNext && char.IsLower(name[index + 1]);

                    if (hasPrevious && (previousIsLowerOrDigit || nextIsLower))
                    {
                        builder.Append('-');
                    }

                    builder.Append(char.ToLowerInvariant(current));
                }
                else
                {
                    builder.Append(current);
                }
            }

            return builder.ToString();
        }
    }
}
