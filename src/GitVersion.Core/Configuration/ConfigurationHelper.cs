using GitVersion.Extensions;

namespace GitVersion.Configuration;

internal class ConfigurationHelper
{
    private string Yaml => this._yaml ??= this._dictionary == null
        ? ConfigurationSerializer.Serialize(this.configuration!)
        : ConfigurationSerializer.Serialize(this._dictionary);
    private string? _yaml;

    internal IReadOnlyDictionary<object, object?> Dictionary
    {
        get
        {
            if (this._dictionary == null)
            {
                this._yaml ??= ConfigurationSerializer.Serialize(this.configuration!);
                this._dictionary = ConfigurationSerializer.Deserialize<Dictionary<object, object?>>(this._yaml);
            }
            return this._dictionary;
        }
    }
    private IReadOnlyDictionary<object, object?>? _dictionary;

    public IGitVersionConfiguration Configuration => this.configuration ??= ConfigurationSerializer.Deserialize<GitVersionConfiguration>(Yaml);
    private IGitVersionConfiguration? configuration;

    internal ConfigurationHelper(string yaml) => this._yaml = yaml.NotNull();

    internal ConfigurationHelper(IReadOnlyDictionary<object, object?> dictionary) => this._dictionary = dictionary.NotNull();

    public ConfigurationHelper(IGitVersionConfiguration configuration) => this.configuration = configuration.NotNull();

    public void Override(IReadOnlyDictionary<object, object?> value)
    {
        value.NotNull();

        if (value.Any())
        {
            var dictionary = Dictionary.ToDictionary(element => element.Key, element => element.Value);
            Merge(dictionary, value);
            this._dictionary = dictionary;
            this._yaml = null;
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
            else if (item.Value is null or string or IList<object>)
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
            else if (item.Value is null or string or IList<object>)
            {
                if (!dictionary.ContainsKey(item.Key))
                {
                    dictionary.Add(item.Key, item.Value);
                }
            }
        }
    }
}
