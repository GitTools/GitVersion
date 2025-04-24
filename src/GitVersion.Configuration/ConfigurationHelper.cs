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
        foreach (var item in dictionary)
        {
            switch (item.Value)
            {
                case IDictionary<object, object?> anotherDictionaryValue:
                    {
                        if (anotherDictionary.TryGetValue(item.Key, out var value) && value is IReadOnlyDictionary<object, object?> dictionaryValue)
                        {
                            Merge(anotherDictionaryValue, dictionaryValue);
                        }

                        break;
                    }
                case null or string or IList<object>:
                    {
                        if (anotherDictionary.TryGetValue(item.Key, out var value))
                        {
                            dictionary[item.Key] = value;
                        }

                        break;
                    }
            }
        }

        foreach (var item in anotherDictionary)
        {
            switch (item.Value)
            {
                case IReadOnlyDictionary<object, object?> when dictionary.ContainsKey(item.Key):
                    continue;
                case IReadOnlyDictionary<object, object?> dictionaryValue:
                    {
                        Dictionary<object, object?> anotherDictionaryValue = [];
                        Merge(anotherDictionaryValue, dictionaryValue);
                        dictionary.Add(item.Key, anotherDictionaryValue);
                        break;
                    }
                case null or string or IList<object>:
                    {
                        if (!dictionary.ContainsKey(item.Key))
                        {
                            dictionary.Add(item.Key, item.Value);
                        }

                        break;
                    }
            }
        }
    }
}
