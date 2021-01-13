using System;
using System.Collections.Generic;

namespace GitVersion.Helpers
{
    public class LambdaKeyComparer<TSource, TKey> : Comparer<TSource> where TSource : class
    {
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IComparer<TKey> _innerComparer;

        public LambdaKeyComparer(
            Func<TSource, TKey> keySelector,
            IComparer<TKey> innerComparer = null)
        {
            _keySelector = keySelector;
            _innerComparer = innerComparer ?? Comparer<TKey>.Default;
        }

        public override int Compare(TSource x, TSource y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            var xKey = _keySelector(x);
            var yKey = _keySelector(y);
            return _innerComparer.Compare(xKey, yKey);
        }
    }
}
