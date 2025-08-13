using System.Globalization;
using System.Text;

namespace GitVersion.Formatting;

internal class LegacyCompositeFormatter : IValueFormatter
{
    public int Priority => 1;

    public bool TryFormat(object? value, string format, out string result) =>
        TryFormat(value, format, CultureInfo.InvariantCulture, out result);

    public bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result)
    {
        result = string.Empty;

        if (!CanFormat(format))
            return false;

        var sections = SplitFormatSections(format);
        var sectionIndex = GetSectionIndex(value, sections.Length);

        if (sectionIndex >= sections.Length)
        {
            result = string.Empty;
            return true;
        }

        var section = sections[sectionIndex];

        if (string.IsNullOrEmpty(section) || IsLiteralString(section))
        {
            result = UnquoteLiteralString(section);
            return true;
        }

        result = FormatValueWithSection(value, section, cultureInfo);
        return true;
    }

    private static bool CanFormat(string format) =>
        !string.IsNullOrEmpty(format) && format.Contains(';') && !format.Contains("??");

    private static string[] SplitFormatSections(string format)
    {
        var sections = new List<string>();
        var currentSection = new StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';

        for (int i = 0; i < format.Length; i++)
        {
            var c = format[i];

            if (!inQuotes && (c == '\'' || c == '"'))
            {
                inQuotes = true;
                quoteChar = c;
                currentSection.Append(c);
            }
            else if (inQuotes && c == quoteChar)
            {
                inQuotes = false;
                currentSection.Append(c);
            }
            else if (!inQuotes && c == ';')
            {
                sections.Add(currentSection.ToString());
                currentSection.Clear();
            }
            else
            {
                currentSection.Append(c);
            }
        }

        sections.Add(currentSection.ToString());
        return sections.ToArray();
    }

    private static int GetSectionIndex(object value, int sectionCount)
    {
        if (sectionCount == 1) return 0;
        if (value == null) return sectionCount >= 3 ? 2 : 0;

        if (IsNumericType(value))
        {
            var numericValue = ConvertToDouble(value);
            if (numericValue > 0) return 0;
            if (numericValue < 0) return sectionCount >= 2 ? 1 : 0;
            return sectionCount >= 3 ? 2 : 0;
        }

        return 0;
    }

    private static bool IsNumericType(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static double ConvertToDouble(object value) => value switch
    {
        byte b => b,
        sbyte sb => sb,
        short s => s,
        ushort us => us,
        int i => i,
        uint ui => ui,
        long l => l,
        ulong ul => ul,
        float f => f,
        double d => d,
        decimal dec => (double)dec,
        _ => 0
    };

    private static bool IsLiteralString(string section)
    {
        if (string.IsNullOrEmpty(section)) return true;
        var trimmed = section.Trim();
        return (trimmed.StartsWith("'") && trimmed.EndsWith("'")) ||
               (trimmed.StartsWith("\"") && trimmed.EndsWith("\""));
    }

    private static string UnquoteLiteralString(string section)
    {
        if (string.IsNullOrEmpty(section)) return string.Empty;
        var trimmed = section.Trim();

        if ((trimmed.StartsWith("'") && trimmed.EndsWith("'")) ||
            (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")))
        {
            return trimmed.Length > 2 ? trimmed.Substring(1, trimmed.Length - 2) : string.Empty;
        }

        return trimmed;
    }

    private static string FormatValueWithSection(object value, string section, IFormatProvider formatProvider)
    {
        if (string.IsNullOrEmpty(section)) return string.Empty;
        if (IsLiteralString(section)) return UnquoteLiteralString(section);

        try
        {
            if (value is IFormattable formattable)
                return formattable.ToString(section, formatProvider ?? CultureInfo.InvariantCulture);

            if (value != null && IsStandardFormatString(section))
                return string.Format(formatProvider ?? CultureInfo.InvariantCulture, "{0:" + section + "}", value);

            return value?.ToString() ?? string.Empty;
        }
        catch (FormatException)
        {
            return section;
        }
    }

    private static bool IsStandardFormatString(string format)
    {
        if (string.IsNullOrEmpty(format)) return false;
        var firstChar = char.ToUpperInvariant(format[0]);
        if ("CDEFGNPXR".Contains(firstChar)) return true;
        return format.All(c => "0123456789.,#".Contains(c));
    }
}
