namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    static class ExtensionMethods
    {
        public static bool IsBranch(this string branchName, string branchNameToCompareAgainst)
        {
            // "develop" == "develop"
            if (string.Equals(branchName, branchNameToCompareAgainst, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // "refs/head/develop" == "develop"
            if (branchName.EndsWith($"/{branchNameToCompareAgainst}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

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
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (source is IList<T> list && list.Count == 1)
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
