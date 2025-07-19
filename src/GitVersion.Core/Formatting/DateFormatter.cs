using System.Globalization;

namespace GitVersion.Formatting;

internal class DateFormatter : InvariantFormatter, IValueFormatter
{
    public int Priority => 2;

    public override bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result)
    {
        result = string.Empty;

        if (value is DateTime dt)
        {
            result = dt.ToString(format, cultureInfo);
            return true;
        }

        if (value is string dateStr && DateTime.TryParse(dateStr, out var parsedDate))
        {
            result = parsedDate.ToString(format, cultureInfo);
            return true;
        }

        return false;
    }
}
