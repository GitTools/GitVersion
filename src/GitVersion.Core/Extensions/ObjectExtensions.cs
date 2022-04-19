namespace GitVersion.Extensions;

public static class ObjectExtensions
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
    internal sealed class ReflectionIgnoreAttribute : Attribute
    {
    }

    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> kvp,
        out TKey key,
        out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }

    public static IEnumerable<KeyValuePair<string, string>> GetProperties(this object obj)
    {
        var type = typeof(string);
        return obj.GetType().GetProperties()
            .Where(p => p.PropertyType == type && !p.GetIndexParameters().Any() && !p.GetCustomAttributes(typeof(ReflectionIgnoreAttribute), false).Any())
            .Select(p => new KeyValuePair<string, string>(p.Name, (string)p.GetValue(obj, null)));
    }
}
