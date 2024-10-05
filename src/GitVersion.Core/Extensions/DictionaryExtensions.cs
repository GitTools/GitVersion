using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GitVersion.Extensions;

public static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> getValue) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dict);
        ArgumentNullException.ThrowIfNull(getValue);

        if (dict.TryGetValue(key, out var value)) return value;
        value = getValue();
        dict.Add(key, value);
        return value;
    }

    public static Regex GetOrAdd(this ConcurrentDictionary<string, Regex> dict, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
    {
        ArgumentNullException.ThrowIfNull(dict);
        ArgumentNullException.ThrowIfNull(pattern);

        return dict.GetOrAdd(pattern, regex => new(regex, RegexOptions.IgnoreCase | RegexOptions.Compiled));
    }
}
