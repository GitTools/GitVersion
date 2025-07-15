using System.Globalization;

namespace GitVersion.Formatting;

internal class NumericFormatter : IValueFormatter
{
    public int Priority => 1;

    public bool TryFormat(object? value, string format, out string result)
    {
        result = string.Empty;

        if (value is not string s)
            return false;

        // Integer formatting
        if (format.All(char.IsDigit) && int.TryParse(s, out var i))
        {
            result = i.ToString(format, CultureInfo.InvariantCulture);
            return true;
        }

        // Hexadecimal formatting
        if (format.StartsWith("X", StringComparison.OrdinalIgnoreCase) && int.TryParse(s, out var hex))
        {
            result = hex.ToString(format, CultureInfo.InvariantCulture);
            return true;
        }

        // Floating point formatting
        if ("FEGNCP".Contains(char.ToUpperInvariant(format[0])) && double.TryParse(s, out var d))
        {
            result = d.ToString(format, CultureInfo.InvariantCulture);
            return true;
        }

        // Decimal formatting
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
        {
            result = dec.ToString(format, CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }
}
