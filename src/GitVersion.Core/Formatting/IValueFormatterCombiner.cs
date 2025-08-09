namespace GitVersion.Formatting;

internal interface IValueFormatterCombiner : IValueFormatter
{
    void RegisterFormatter(IValueFormatter formatter);

    void RemoveFormatter<T>() where T : IValueFormatter;
}
