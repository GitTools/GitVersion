using System.Globalization;

namespace GitVersion.Formatting;

internal class NumericFormatter : InvariantFormatter, IValueFormatter
{
    public int Priority => 1;

    public override bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result)
    {
        result = string.Empty;

        if (value is not string s)
            return false;

        // Integer formatting
        if (format.All(char.IsDigit) && int.TryParse(s, out var i))
        {
            result = i.ToString(format, cultureInfo);
            return true;
        }

        // Hexadecimal formatting
        if (format.StartsWith("X", StringComparison.OrdinalIgnoreCase) && int.TryParse(s, out var hex))
        {
            result = hex.ToString(format, cultureInfo);
            return true;
        }

        // Floating point formatting
        if ("FEGNCP".Contains(char.ToUpperInvariant(format[0])) && double.TryParse(s, out var d))
        {
            result = d.ToString(format, cultureInfo);
            return true;
        }

        // Decimal formatting
        if (decimal.TryParse(s, NumberStyles.Any, cultureInfo, out var dec))
        {
            result = dec.ToString(format, cultureInfo);
            return true;
        }

        return false;
    }
}
