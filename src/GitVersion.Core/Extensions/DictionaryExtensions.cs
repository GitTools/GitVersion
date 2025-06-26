namespace GitVersion.Extensions;

internal static class DictionaryExtensions
{
    internal static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> getValue) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dict);
        ArgumentNullException.ThrowIfNull(getValue);

        if (dict.TryGetValue(key, out var value)) return value;
        value = getValue();
        dict.Add(key, value);
        return value;
    }
}
