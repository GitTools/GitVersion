namespace GitVersion.Formatting;

internal interface IValueFormatter
{
    bool TryFormat(object? value, string format, out string result);

    /// <summary>
    ///   Lower number = higher priority
    /// </summary>
    int Priority { get; }
}
