using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using GitVersion.Core;

namespace GitVersion.Extensions;

public static class StringExtensions
{
    public static void AppendLineFormat(this StringBuilder stringBuilder, string format, params object[] args)
    {
        stringBuilder.AppendFormat(format, args);
        stringBuilder.AppendLine();
    }

    public static string RegexReplace(this string input, string pattern, string replace)
    {
        var regex = RegexPatterns.Cache.GetOrAdd(pattern);
        return regex.Replace(input, replace);
    }

    public static bool IsEquivalentTo(this string self, string? other) =>
        string.Equals(self, other, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc cref="string.IsNullOrEmpty"/>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) => string.IsNullOrEmpty(value);

    /// <inheritdoc cref="string.IsNullOrWhiteSpace"/>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value) => string.IsNullOrWhiteSpace(value);

    public static bool IsEmpty([NotNullWhen(false)] this string? value) => string.Empty.Equals(value);

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
