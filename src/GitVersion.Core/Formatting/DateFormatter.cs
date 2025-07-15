using System.Globalization;

namespace GitVersion.Formatting;

internal class DateFormatter : IValueFormatter
{
    public int Priority => 2;

    public bool TryFormat(object? value, string format, out string result)
    {
        result = string.Empty;

        if (value is DateTime dt && format.StartsWith("date:"))
        {
            var dateFormat = RemoveDatePrefix(format);
            result = dt.ToString(dateFormat, CultureInfo.InvariantCulture);
            return true;
        }

        if (value is string dateStr && DateTime.TryParse(dateStr, out var parsedDate) && format.StartsWith("date:"))
        {
            var dateFormat = format.Substring(5);
            result = parsedDate.ToString(dateFormat, CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }

    private static string RemoveDatePrefix(string format) => format.Substring(5);
}
