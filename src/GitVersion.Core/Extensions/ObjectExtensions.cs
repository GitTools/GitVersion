namespace GitVersion.Extensions;

public static class ObjectExtensions
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
    internal sealed class ReflectionIgnoreAttribute : Attribute;

    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> kvp,
        out TKey key,
        out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }

    public static Dictionary<string, string> GetProperties(this object obj)
    {
        var type = typeof(string);
        return obj.GetType().GetProperties()
            .Where(p => p.PropertyType == type && p.GetIndexParameters().Length == 0 && p.GetCustomAttributes(typeof(ReflectionIgnoreAttribute), false).Length == 0)
            .ToDictionary(p => p.Name, p => Convert.ToString(p.GetValue(obj, null)) ?? string.Empty);
    }
}
