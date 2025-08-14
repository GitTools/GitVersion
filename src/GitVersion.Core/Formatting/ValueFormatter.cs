using System.Globalization;

namespace GitVersion.Formatting;

internal class ValueFormatter : InvariantFormatter, IValueFormatterCombiner
{
    private readonly List<IValueFormatter> formatters;

    internal static IValueFormatter Default { get; } = new ValueFormatter();

    public int Priority => 0;

    internal ValueFormatter()
        => formatters =
        [
            new LegacyCompositeFormatter(),
            new StringFormatter(),
            new FormattableFormatter(),
            new NumericFormatter(),
            new DateFormatter()
        ];

    public override bool TryFormat(object? value, string format, CultureInfo cultureInfo, out string result)
    {
        result = string.Empty;

        // Allow formatters to handle null values (e.g., legacy composite formatter for zero sections)
        foreach (var formatter in formatters.OrderBy(f => f.Priority))
        {
            if (formatter.TryFormat(value, format, out result))
                return true;
        }

        // Only return false if no formatter could handle it
        if (value is null)
        {
            return false;
        }

        return false;
    }

    void IValueFormatterCombiner.RegisterFormatter(IValueFormatter formatter) => formatters.Add(formatter);

    void IValueFormatterCombiner.RemoveFormatter<T>() => formatters.RemoveAll(f => f is T);
}
