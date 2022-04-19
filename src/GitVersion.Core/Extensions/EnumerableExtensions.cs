namespace GitVersion.Extensions;

public static class EnumerableExtensions
{
    public static T? OnlyOrDefault<T>(this IEnumerable<T> source)
    {
        switch (source)
        {
            case null:
                throw new ArgumentNullException(nameof(source));
            case IList<T> { Count: 1 } list:
                return list[0];
        }

        using var e = source.GetEnumerator();
        if (!e.MoveNext())
            return default;
        var current = e.Current;
        return !e.MoveNext() ? current : default;
    }
}
