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

    internal static string PascalCase(this string input, CultureInfo cultureInfo)
    {
        var sb = new StringBuilder(input.Length);
        var capitalizeNext = true;

        foreach (var c in input)
        {
            if (!char.IsLetterOrDigit(c))
            {
                capitalizeNext = true;
                continue;
            }

            sb.Append(capitalizeNext ? cultureInfo.TextInfo.ToUpper(c) : cultureInfo.TextInfo.ToLower(c));
            capitalizeNext = false;
        }

        return sb.ToString();
    }
}
