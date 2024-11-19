namespace GitVersion.Extensions;

public static class EnumerableExtensions
{
    public static T? OnlyOrDefault<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is IList<T> { Count: 1 } list)
        {
            return list[0];
        }

        using var e = source.GetEnumerator();
        if (!e.MoveNext())
            return default;
        var current = e.Current;
        return !e.MoveNext() ? current : default;
    }

    public static T SingleOfType<T>(this IEnumerable source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.OfType<T>().Single();
    }

    public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        source.NotNull();

        foreach (var item in items.NotNull())
        {
            source.Add(item);
        }
    }
}
