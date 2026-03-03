using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using GitVersion.Core;

namespace GitVersion.Extensions;

#pragma warning disable S2325, S1144, S4136, S1199
public static class StringExtensions
{
    public static void AppendLineFormat(this StringBuilder stringBuilder, string format, params object[] args)
    {
        stringBuilder.AppendFormat(format, args);
        stringBuilder.AppendLine();
    }

    extension(string input)
    {
        public string RegexReplace(string pattern, string replace)
        {
            var regex = RegexPatterns.Cache.GetOrAdd(pattern);
            return regex.Replace(input, replace);
        }

        public bool IsEquivalentTo(string? other) =>
            string.Equals(input, other, StringComparison.OrdinalIgnoreCase);

        public string WithPrefixIfNotNullOrEmpty(string prefix)
            => string.IsNullOrEmpty(input) ? input : prefix + input;
    }

    extension([NotNullWhen(false)] string? value)
    {
        /// <inheritdoc cref="string.IsNullOrEmpty"/>
        public bool IsNullOrEmpty() => string.IsNullOrEmpty(value);

        /// <inheritdoc cref="string.IsNullOrWhiteSpace"/>
        public bool IsNullOrWhiteSpace() => string.IsNullOrWhiteSpace(value);
    }

#pragma warning disable S108
    extension([NotNull] string? value)
    {
        public string NotNullOrEmpty([CallerArgumentExpression(nameof(value))] string name = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("The parameter is null or empty.", name);
            }

            return value;
        }

        public string NotNullOrWhitespace([CallerArgumentExpression(nameof(value))] string name = "")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("The parameter is null or empty or contains only white space.", name);
            }

            return value;
        }
    }
#pragma warning restore S108

    internal static string ToPascalCase(this TextInfo textInfo, string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder(input.Length);
        var capitalizeNext = true;

        foreach (var c in input)
        {
            if (!char.IsLetterOrDigit(c))
            {
                capitalizeNext = true;
                continue;
            }

            sb.Append(capitalizeNext ? textInfo.ToUpper(c) : textInfo.ToLower(c));
            capitalizeNext = false;
        }

        return sb.ToString();
    }
}
#pragma warning restore S2325, S1144, S4136, S1199
