using System.Globalization;
using GitVersion.Extensions;

namespace GitVersion.Formatting;

internal class StringFormatter : InvariantFormatter, IValueFormatter
{
    public int Priority => 2;

    public override bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result)
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
                result = cultureInfo.TextInfo.ToUpper(stringValue);
                return true;
            case "l":
                result = cultureInfo.TextInfo.ToLower(stringValue);
                return true;
            case "t":
                result = cultureInfo.TextInfo.ToTitleCase(cultureInfo.TextInfo.ToLower(stringValue));
                return true;
            case "s":
                if (stringValue.Length == 1)
                    result = cultureInfo.TextInfo.ToUpper(stringValue);
                else
                {
                    result = cultureInfo.TextInfo.ToUpper(stringValue[0]) + cultureInfo.TextInfo.ToLower(stringValue[1..]);
                }

                return true;
            case "c":
                result = stringValue.PascalCase(cultureInfo);
                return true;
            default:
                result = string.Empty;
                return false;
        }
    }
}
