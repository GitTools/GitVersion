namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using JetBrains.Annotations;

    static class ExtensionMethods
    {
        [StringFormatMethod("format")]
        public static void AppendLineFormat(this StringBuilder stringBuilder, string format, params object[] args)
        {
            stringBuilder.AppendFormat(format, args);
            stringBuilder.AppendLine();
        }

        public static string RegexReplace(this string input, string pattern, string replace, RegexOptions options = RegexOptions.None)
        {
            return Regex.Replace(input, pattern, replace, options);
        }

        public static T OnlyOrDefault<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            var list = source as IList<T>;

            if (list != null && list.Count == 1)
            {
                return list[0];
            }

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return default(T);
                var result = e.Current;
                if (!e.MoveNext()) return result;
            }

            return default(T);
        }
    }
}