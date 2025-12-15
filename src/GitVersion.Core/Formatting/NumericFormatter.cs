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
        if (format.All(char.IsDigit) && int.TryParse(s, NumberStyles.Integer, cultureInfo, out var i))
        {
            result = i.ToString(format, cultureInfo);
            return true;
        }

        // Integer formatting with precision specifier
        if ("BDX".Contains(char.ToUpperInvariant(format[0])) && int.TryParse(s, NumberStyles.Integer, cultureInfo, out var n))
        {
            result = n.ToString(format, cultureInfo);
            return true;
        }

        // Floating point formatting
        if ("FEGNCP".Contains(char.ToUpperInvariant(format[0])) && double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, cultureInfo, out var d))
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
