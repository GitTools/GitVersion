using SharpYaml;

namespace GitVersion.Configuration;

internal class ConfigurationSerializer : IConfigurationSerializer
{
    private static readonly ConfigurationYamlContext GeneratedContext = ConfigurationYamlContext.Default;

    private static readonly YamlSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = HyphenatedJsonNamingPolicy.Instance,
        DefaultIgnoreCondition = YamlIgnoreCondition.WhenWritingNull,
        Converters = [VersionStrategiesConverter.Instance]
    };

    public T Deserialize<T>(string input)
    {
        if (typeof(T) == typeof(Dictionary<object, object?>))
        {
            var graph = YamlSerializer.Deserialize<Dictionary<string, object?>>(input, SerializerOptions);
            return (T)(object)ConvertToObjectDictionary(graph);
        }

        if (typeof(T) == typeof(GitVersionConfiguration))
        {
            try
            {
                return (T)(object)YamlSerializer.Deserialize<GitVersionConfiguration>(input, GeneratedContext)!;
            }
            catch (Exception exception) when (exception is not YamlException)
            {
                throw new YamlException(exception.Message, exception);
            }
        }

        return YamlSerializer.Deserialize<T>(input, SerializerOptions)!;
    }

    public string Serialize(object graph)
        => YamlSerializer.Serialize(graph, SerializerOptions);

    public IGitVersionConfiguration? ReadConfiguration(string input) => Deserialize<GitVersionConfiguration?>(input);

    private static Dictionary<object, object?> ConvertToObjectDictionary(IReadOnlyDictionary<string, object?>? source)
    {
        if (source is null) return [];

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

    private sealed class HyphenatedJsonNamingPolicy : JsonNamingPolicy
    {
        public static JsonNamingPolicy Instance { get; } = new HyphenatedJsonNamingPolicy();

        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

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
