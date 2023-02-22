using GitVersion.Extensions;

namespace GitVersion.Configuration;

internal class ConfigurationHelper
{
    public string Yaml
    {
        get
        {
            if (this.yaml == null)
            {
                if (this.dictionary == null)
                {
                    this.yaml = ConfigurationSerializer.Serialize(this.configuration!);
                }
                else
                {
                    this.yaml = ConfigurationSerializer.Serialize(this.dictionary);
                }
            }
            return this.yaml;
        }
    }
    private string? yaml;

    public IReadOnlyDictionary<object, object?> Dictionary
    {
        get
        {
            if (this.dictionary == null)
            {
                this.yaml ??= ConfigurationSerializer.Serialize(this.configuration!);
                this.dictionary = ConfigurationSerializer.Deserialize<Dictionary<object, object?>>(this.yaml!);
            }
            return this.dictionary;
        }
    }
    private IReadOnlyDictionary<object, object?>? dictionary;

    public GitVersionConfiguration Configuration => this.configuration ??= ConfigurationSerializer.Deserialize<GitVersionConfiguration>(Yaml);
    private GitVersionConfiguration? configuration;

    public ConfigurationHelper(string yaml) => this.yaml = yaml.NotNull();

    public ConfigurationHelper(IReadOnlyDictionary<object, object?> dictionary) => this.dictionary = dictionary.NotNull();

    public ConfigurationHelper(GitVersionConfiguration configuration) => this.configuration = configuration.NotNull();

    public void Override(IReadOnlyDictionary<object, object?> value)
    {
        value.NotNull();

        if (value.Any())
        {
            var dictionary = Dictionary.ToDictionary(element => element.Key, element => element.Value);
            Merge(dictionary, value);
            this.dictionary = dictionary;
            this.yaml = null;
            this.configuration = null;
        }
    }

    private static void Merge(IDictionary<object, object?> dictionary, IReadOnlyDictionary<object, object?> anotherDictionary)
    {
        foreach (var item in dictionary)
        {
            if (item.Value is IDictionary<object, object?> anotherDictionaryValue)
            {
                if (anotherDictionary.TryGetValue(item.Key, out var value) && value is IReadOnlyDictionary<object, object?> dictionaryValue)
                {
                    Merge(anotherDictionaryValue, dictionaryValue);
                }
            }
            else if (item.Value is null || item.Value is string || item.Value is IList<object>)
            {
                if (anotherDictionary.TryGetValue(item.Key, out var value))
                {
                    dictionary[item.Key] = value;
                }
            }
        }

        foreach (var item in anotherDictionary)
        {
            if (item.Value is IReadOnlyDictionary<object, object?> dictionaryValue)
            {
                if (!dictionary.ContainsKey(item.Key))
                {
                    Dictionary<object, object?> anotherDictionaryValue = new();
                    Merge(anotherDictionaryValue, dictionaryValue);
                    dictionary.Add(item.Key, anotherDictionaryValue);
                }
            }
            else if (item.Value is null || item.Value is string || item.Value is IList<object>)
            {
                if (!dictionary.ContainsKey(item.Key))
                {
                    dictionary.Add(item.Key, item.Value);
                }
            }
        }
    }
}
