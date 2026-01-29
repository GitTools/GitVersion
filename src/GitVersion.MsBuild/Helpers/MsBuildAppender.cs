using GitVersion.Helpers;
using Microsoft.Build.Utilities;

namespace GitVersion.MsBuild;

/// <summary>
/// A Serilog sink that forwards log messages to MSBuild's TaskLoggingHelper.
/// </summary>
internal sealed class MsBuildLoggerProvider(TaskLoggingHelper taskLog) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new MsBuildLogger(taskLog);

    public void Dispose()
    {
        // Nothing to dispose
    }
}

#pragma warning disable S2325
internal sealed class MsBuildLogger(TaskLoggingHelper taskLog) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            var message = formatter(state, exception);
            var contents = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t\t{message}{FileSystemHelper.Path.NewLine}";

            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    taskLog.LogError(contents);
                    break;
                case LogLevel.Warning:
                    taskLog.LogWarning(contents);
                    break;
                case LogLevel.Information:
                case LogLevel.Debug:
                case LogLevel.Trace:
                    taskLog.LogMessage(contents);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }
        catch
        {
            // Ignore logging errors
        }
    }
}
#pragma warning restore S2325

