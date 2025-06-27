using System.Globalization;

namespace GitVersion.Helpers;

internal class FormattableFormatter : IValueFormatter
{
    public int Priority => 2;

    public bool TryFormat(object? value, string format, out string result)
    {
        result = string.Empty;

        if (string.IsNullOrWhiteSpace(format))
            return false;

        if (IsBlockedFormat(format))
        {
            result = $"Format '{format}' is not supported in {nameof(FormattableFormatter)}";
            return false;
        }

        if (value is IFormattable formattable)
        {
            try
            {
                result = formattable.ToString(format, CultureInfo.InvariantCulture);
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

    private static bool IsBlockedFormat(string format) =>
        format.Equals("C", StringComparison.OrdinalIgnoreCase) ||
        format.Equals("P", StringComparison.OrdinalIgnoreCase) ||
        format.StartsWith("N", StringComparison.OrdinalIgnoreCase);
}
