namespace GitVersion.Formatting;

internal static class ValueFormatter
{
    private static readonly List<IValueFormatter> formatters =
    [
        new StringFormatter(),
        new FormattableFormatter(),
        new NumericFormatter(),
        new DateFormatter()
    ];

    public static bool TryFormat(object? value, string format, out string result)
    {
        result = string.Empty;

        if (value is null)
            return false;

        foreach (var formatter in formatters.OrderBy(f => f.Priority))
        {
            if (formatter.TryFormat(value, format, out result))
                return true;
        }

        return false;
    }

    public static void RegisterFormatter(IValueFormatter formatter) => formatters.Add(formatter);

    public static void RemoveFormatter<T>() where T : IValueFormatter => formatters.RemoveAll(f => f is T);
}
