using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace GitVersion.Extensions;

/// <summary>Extension methods on <see cref="string"/> and <see cref="StringBuilder"/> for common string manipulations.</summary>
#pragma warning disable S2325, S1144, S4136, S1199
public static class StringExtensions
{
    /// <summary>Appends a formatted line (format + newline) to the <see cref="StringBuilder"/>.</summary>
    public static void AppendLineFormat(this StringBuilder stringBuilder, string format, params object[] args)
    {
        stringBuilder.AppendFormat(format, args);
        stringBuilder.AppendLine();
    }

    extension(string input)
    {
        /// <summary>Replaces all occurrences of <paramref name="pattern"/> in <paramref name="input"/> with <paramref name="replace"/> using the regex cache.</summary>
        public string RegexReplace(string pattern, string replace)
        {
            var regex = RegexPatterns.Cache.GetOrAdd(pattern);
            return regex.Replace(input, replace);
        }

        /// <summary>Returns <see langword="true"/> when <paramref name="input"/> and <paramref name="other"/> are equal under ordinal case-insensitive comparison.</summary>
        public bool IsEquivalentTo(string? other) =>
            string.Equals(input, other, StringComparison.OrdinalIgnoreCase);

        /// <summary>Prepends <paramref name="prefix"/> to <paramref name="input"/> when the value is non-empty; returns the original value otherwise.</summary>
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
        /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="value"/> is <see langword="null"/> or empty; otherwise returns the value.</summary>
        public string NotNullOrEmpty([CallerArgumentExpression(nameof(value))] string name = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("The parameter is null or empty.", name);
            }

            return value;
        }

        /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="value"/> is <see langword="null"/>, empty, or whitespace; otherwise returns the value.</summary>
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
