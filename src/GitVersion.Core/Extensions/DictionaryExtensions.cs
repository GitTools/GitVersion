namespace GitVersion.Extensions;

public static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> getValue)
    {
        if (dict is null) throw new ArgumentNullException(nameof(dict));
        if (getValue is null) throw new ArgumentNullException(nameof(getValue));
        if (!dict.TryGetValue(key, out TValue value))
        {
            value = getValue();
            dict.Add(key, value);
        }
        return value;
    }
}
