namespace GitVersion.Logging;

/// <summary>Provides structured logging for GitVersion, supporting multiple verbosity levels and pluggable appenders.</summary>
public interface ILog
{
    /// <summary>Gets or sets the minimum verbosity level at which messages are written.</summary>
    Verbosity Verbosity { get; set; }

    /// <summary>Writes a formatted log message at the given <paramref name="verbosity"/> and <paramref name="level"/>.</summary>
    void Write(Verbosity verbosity, LogLevel level, string format, params object?[] args);

    /// <summary>Returns a disposable scope that indents subsequent log output and labels it with <paramref name="operationDescription"/>.</summary>
    IDisposable IndentLog(string operationDescription);

    /// <summary>Registers an additional <see cref="ILogAppender"/> that will receive all log messages.</summary>
    void AddLogAppender(ILogAppender logAppender);

    /// <summary>Writes a visual separator line to the log output.</summary>
    void Separator();
}
