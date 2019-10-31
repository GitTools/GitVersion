using System;
using System.Collections.Generic;

namespace GitVersion.Extensions
{
    public static class EnumerableExtensions
    {
        public static T OnlyOrDefault<T>(this IEnumerable<T> source)
        {
            switch (source)
            {
                case null:
                    throw new ArgumentNullException(nameof(source));
                case IList<T> list when list.Count == 1:
                    return list[0];
            }

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    return default;
                var current = e.Current;
                if (!e.MoveNext())
                    return current;
            }

            return default;
        }
    }
}
