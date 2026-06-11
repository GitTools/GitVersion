namespace GitVersion.Helpers;

/// <summary>A <see cref="Comparer{T}"/> that delegates comparison to a key-selector function, with an optional inner comparer for the key type.</summary>
public class LambdaKeyComparer<TSource, TKey>(
    Func<TSource, TKey> keySelector,
    IComparer<TKey>? innerComparer = null)
    : Comparer<TSource>
    where TSource : class
{
    private readonly IComparer<TKey> innerComparer = innerComparer ?? Comparer<TKey>.Default;

    /// <summary>Compares <paramref name="x"/> and <paramref name="y"/> by applying the key selector and delegating to the inner comparer.</summary>
    public override int Compare(TSource? x, TSource? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (x == null)
            return -1;
        if (y == null)
            return 1;

        var xKey = keySelector(x);
        var yKey = keySelector(y);
        return this.innerComparer.Compare(xKey, yKey);
    }
}
