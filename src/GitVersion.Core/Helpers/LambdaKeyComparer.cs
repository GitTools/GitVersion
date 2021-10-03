namespace GitVersion.Helpers;

public class LambdaKeyComparer<TSource, TKey> : Comparer<TSource> where TSource : class
{
    private readonly Func<TSource, TKey> keySelector;
    private readonly IComparer<TKey> innerComparer;

    public LambdaKeyComparer(
        Func<TSource, TKey> keySelector,
        IComparer<TKey>? innerComparer = null)
    {
        this.keySelector = keySelector;
        this.innerComparer = innerComparer ?? Comparer<TKey>.Default;
    }

    public override int Compare(TSource x, TSource y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (x == null)
            return -1;
        if (y == null)
            return 1;

        var xKey = this.keySelector(x);
        var yKey = this.keySelector(y);
        return this.innerComparer.Compare(xKey, yKey);
    }
}
