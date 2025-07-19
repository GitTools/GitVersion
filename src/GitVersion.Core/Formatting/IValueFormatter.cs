using System.Globalization;

namespace GitVersion.Formatting;

internal interface IValueFormatter
{
    bool TryFormat(object? value, string format, out string result);

    bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result);

    /// <summary>
    ///   Lower number = higher priority
    /// </summary>
    int Priority { get; }
}
