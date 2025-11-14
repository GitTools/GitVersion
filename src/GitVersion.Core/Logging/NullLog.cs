using GitVersion.Helpers;

namespace GitVersion.Logging;

internal sealed class NullLog : ILog
{
    public Verbosity Verbosity { get; set; }

    public void Write(Verbosity verbosity, LogLevel level, string format, params object?[] args)
    {
    }

    public IDisposable IndentLog(string operationDescription) => Disposable.Empty;

    public void AddLogAppender(ILogAppender logAppender)
    {
    }

    public void Separator()
    {
    }

    public string? Indent { get; set; }
}
