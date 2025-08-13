using System.Globalization;

namespace GitVersion.Formatting;

internal class LegacyCompositeFormatter : IValueFormatter
{
    public int Priority => 1;

    public bool TryFormat(object? value, string format, out string result) =>
        TryFormat(value, format, CultureInfo.InvariantCulture, out result);

    public bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result)
    {
        result = string.Empty;

        if (!HasLegacySyntax(format))
            return false;

        var sections = ParseSections(format);
        var index = GetSectionIndex(value, sections.Length);

        if (index >= sections.Length)
            return true;

        var section = sections[index];
        result = IsQuotedLiteral(section)
            ? UnquoteString(section)
            : FormatWithSection(value, section, cultureInfo);

        return true;
    }

    private static bool HasLegacySyntax(string format) =>
        !string.IsNullOrEmpty(format) && format.Contains(';') && !format.Contains("??");

    private static string[] ParseSections(string format)
    {
        var sections = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';

        foreach (var c in format)
        {
            if (!inQuotes && (c == '\'' || c == '"'))
            {
                inQuotes = true;
                quoteChar = c;
            }
            else if (inQuotes && c == quoteChar)
            {
                inQuotes = false;
            }
            else if (!inQuotes && c == ';')
            {
                sections.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        sections.Add(current.ToString());
        return [.. sections];
    }

    private static int GetSectionIndex(object? value, int sectionCount)
    {
        if (sectionCount == 1) return 0;
        if (value == null) return sectionCount >= 3 ? 2 : 0;

        if (!IsNumeric(value)) return 0;

        var num = Convert.ToDouble(value);
        return num switch
        {
            > 0 => 0,
            < 0 when sectionCount >= 2 => 1,
            0 when sectionCount >= 3 => 2,
            _ => 0
        };
    }

    private static bool IsNumeric(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static bool IsQuotedLiteral(string section)
    {
        if (string.IsNullOrEmpty(section)) return true;
        var trimmed = section.Trim();
        return (trimmed.StartsWith('\'') && trimmed.EndsWith('\'')) ||
               (trimmed.StartsWith('"') && trimmed.EndsWith('"'));
    }

    private static string UnquoteString(string section)
    {
        if (string.IsNullOrEmpty(section)) return string.Empty;
        var trimmed = section.Trim();

        return IsQuoted(trimmed) && trimmed.Length > 2
            ? trimmed[1..^1]
            : trimmed;

        static bool IsQuoted(string s) =>
            (s.StartsWith('\'') && s.EndsWith('\'')) || (s.StartsWith('"') && s.EndsWith('"'));
    }

    private static string FormatWithSection(object? value, string section, IFormatProvider formatProvider)
    {
        if (string.IsNullOrEmpty(section)) return string.Empty;
        if (IsQuotedLiteral(section)) return UnquoteString(section);

        try
        {
            return value switch
            {
                IFormattable formattable => formattable.ToString(section, formatProvider),
                not null when IsValidFormatString(section) =>
                    string.Format(formatProvider, "{0:" + section + "}", value),
                _ => value?.ToString() ?? string.Empty
            };
        }
        catch (FormatException)
        {
            return section;
        }
    }

    private static bool IsValidFormatString(string format)
    {
        if (string.IsNullOrEmpty(format)) return false;

        var firstChar = char.ToUpperInvariant(format[0]);
        return "CDEFGNPXR".Contains(firstChar) ||
               format.All(c => "0123456789.,#".Contains(c));
    }
}
