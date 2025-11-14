namespace GitVersion.Extensions;

internal static class DictionaryExtensions
{
    extension<TKey, TValue>(Dictionary<TKey, TValue> dict) where TKey : notnull
    {
        internal TValue GetOrAdd(TKey key, Func<TValue> getValue)
        {
            ArgumentNullException.ThrowIfNull(dict);
            ArgumentNullException.ThrowIfNull(getValue);

            if (dict.TryGetValue(key, out var value)) return value;
            value = getValue();
            dict.Add(key, value);
            return value;
        }
    }
}
