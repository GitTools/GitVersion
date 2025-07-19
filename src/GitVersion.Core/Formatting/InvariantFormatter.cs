using System.Globalization;

namespace GitVersion.Formatting;

internal abstract class InvariantFormatter
{
    public bool TryFormat(object? value, string format, out string result)
        => TryFormat(value, format, CultureInfo.InvariantCulture, out result);

    public abstract bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result);
}
