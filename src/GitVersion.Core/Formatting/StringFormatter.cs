using System.Globalization;
using GitVersion.Extensions;

namespace GitVersion.Formatting;

internal class StringFormatter : IValueFormatter
{
    public int Priority => 2;

    public bool TryFormat(object? value, string format, out string result)
    {
        if (value is not string stringValue)
        {
            result = string.Empty;
            return false;
        }

        if (string.IsNullOrWhiteSpace(stringValue))
        {
            result = string.Empty;
            return true;
        }

        switch (format)
        {
            case "u":
                result = stringValue.ToUpperInvariant();
                return true;
            case "l":
                result = stringValue.ToLowerInvariant();
                return true;
            case "t":
                result = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(stringValue.ToLowerInvariant());
                return true;
            case "s":
                if (stringValue.Length == 1)
                    result = stringValue.ToUpperInvariant();
                else
                {
                    result = char.ToUpperInvariant(stringValue[0]) + stringValue[1..].ToLowerInvariant();
                }

                return true;
            case "c":
                result = stringValue.PascalCase();
                return true;
            default:
                result = string.Empty;
                return false;
        }
    }
}
