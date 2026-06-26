using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GitVersion.Extensions;

/// <summary>Extension methods on <see cref="string"/> and <see cref="StringBuilder"/> for common string manipulations.</summary>
public static class StringExtensions
{
    /// <summary>Appends a formatted line (format + newline) to the <see cref="StringBuilder"/>.</summary>
    public static void AppendLineFormat(this StringBuilder stringBuilder, string format, params object[] args)
    {
        stringBuilder.AppendFormat(format, args);
        stringBuilder.AppendLine();
    }

    /// <summary>Replaces all occurrences of <paramref name="pattern"/> in <paramref name="input"/> with <paramref name="replace"/> using the regex cache.</summary>
    public static string RegexReplace(this string input, string pattern, string replace)
    {
        var regex = RegexPatterns.Cache.GetOrAdd(pattern);
        return regex.Replace(input, replace);
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="self"/> and <paramref name="other"/> are equal under ordinal case-insensitive comparison.</summary>
    public static bool IsEquivalentTo(this string self, string? other) =>
        string.Equals(self, other, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc cref="string.IsNullOrEmpty"/>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) => string.IsNullOrEmpty(value);

    /// <inheritdoc cref="string.IsNullOrWhiteSpace"/>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>Returns <see langword="true"/> when <paramref name="value"/> is exactly <see cref="string.Empty"/>.</summary>
    public static bool IsEmpty([NotNullWhen(false)] this string? value) => string.Empty.Equals(value);

    /// <summary>Prepends <paramref name="prefix"/> to <paramref name="value"/> when the value is non-empty; returns the original value otherwise.</summary>
    public static string WithPrefixIfNotNullOrEmpty(this string value, string prefix)
        => string.IsNullOrEmpty(value) ? value : prefix + value;

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
