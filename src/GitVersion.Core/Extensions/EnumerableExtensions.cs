namespace GitVersion.Extensions;

/// <summary>Extension methods that augment <see cref="IEnumerable{T}"/> and related collection types.</summary>
public static class EnumerableExtensions
{
    /// <summary>Returns the single element of the sequence, or <see langword="default"/> if the sequence is empty or contains more than one element.</summary>
    public static T? OnlyOrDefault<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is IList<T> { Count: 1 } list)
        {
            return list[0];
        }

        using var e = source.GetEnumerator();
        if (!e.MoveNext())
        {
            return default;
        }

        var current = e.Current;
        return !e.MoveNext() ? current : default;
    }

    /// <summary>Returns the single element of type <typeparamref name="T"/> from a non-generic sequence, throwing if not exactly one exists.</summary>
    public static T SingleOfType<T>(this IEnumerable source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.OfType<T>().Single();
    }

    /// <summary>Appends all elements of <paramref name="items"/> to <paramref name="source"/>.</summary>
    public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        source.NotNull();

        foreach (var item in items.NotNull())
        {
            source.Add(item);
        }
    }
}
