using GitVersion.Extensions;

namespace GitVersion.Configuration;

internal class ConfigurationHelper
{
    private static ConfigurationSerializer Serializer => new();
    private string Yaml => this.yaml ??= this.dictionary == null
        ? Serializer.Serialize(this.configuration!)
        : Serializer.Serialize(this.dictionary);
    private string? yaml;

    internal IReadOnlyDictionary<object, object?> Dictionary
    {
        get
        {
            if (this.dictionary != null) return this.dictionary;
            this.yaml ??= Serializer.Serialize(this.configuration!);
            this.dictionary = Serializer.Deserialize<Dictionary<object, object?>>(this.yaml);
            return this.dictionary;
        }
    }
    private IReadOnlyDictionary<object, object?>? dictionary;

    public IGitVersionConfiguration Configuration => this.configuration ??= Serializer.Deserialize<GitVersionConfiguration>(Yaml);
    private IGitVersionConfiguration? configuration;

    internal ConfigurationHelper(string yaml) => this.yaml = yaml.NotNull();

    internal ConfigurationHelper(IReadOnlyDictionary<object, object?> dictionary) => this.dictionary = dictionary.NotNull();

    public ConfigurationHelper(IGitVersionConfiguration configuration) => this.configuration = configuration.NotNull();

    public void Override(IReadOnlyDictionary<object, object?> value)
    {
        value.NotNull();

        if (!value.Any()) return;
        var map = Dictionary.ToDictionary(element => element.Key, element => element.Value);
        Merge(map, value);
        this.dictionary = map;
        this.yaml = null;
        this.configuration = null;
    }

    private static void Merge(IDictionary<object, object?> dictionary, IReadOnlyDictionary<object, object?> anotherDictionary)
    {
        foreach (var (key, sourceValue) in anotherDictionary)
        {
            if (dictionary.TryGetValue(key, out var currentValue)
                && currentValue is IDictionary<object, object?> currentDictionary
                && sourceValue is IReadOnlyDictionary<object, object?> sourceDictionary)
            {
                Merge(currentDictionary, sourceDictionary);
                continue;
            }

            dictionary[key] = sourceValue is IReadOnlyDictionary<object, object?> nestedDictionary
                ? CloneDictionary(nestedDictionary)
                : sourceValue;
        }
    }

    private static Dictionary<object, object?> CloneDictionary(IReadOnlyDictionary<object, object?> dictionary)
    {
        Dictionary<object, object?> cloned = [];

        foreach (var (key, value) in dictionary)
        {
            cloned[key] = value is IReadOnlyDictionary<object, object?> nestedDictionary
                ? CloneDictionary(nestedDictionary)
                : value;
        }

        return cloned;
    }
}
