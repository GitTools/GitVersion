using System.Globalization;

namespace GitVersion.Formatting;

internal class FormattableFormatter : InvariantFormatter, IValueFormatter
{
    public int Priority => 2;

    public override bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result)
    {
        result = string.Empty;

        if (string.IsNullOrWhiteSpace(format))
            return false;

        if (value is IFormattable formattable)
        {
            try
            {
                result = formattable.ToString(format, cultureInfo);
                return true;
            }
            catch (FormatException)
            {
                result = $"Format '{format}' is not supported in {nameof(FormattableFormatter)}";
                return false;
            }
        }

        return false;
    }
}
