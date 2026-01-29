using Serilog.Core;
using Serilog.Events;

namespace GitVersion.Logging;

/// <summary>
/// Serilog enricher that adds dynamic log file path property and controls log level based on verbosity.
/// </summary>
internal sealed class LoggingEnricher : ILogEventEnricher
{
    /// <summary>
    /// The log level switch that controls the minimum log level.
    /// </summary>
    public static readonly LoggingLevelSwitch LogLevelSwitch = new();

    /// <summary>
    /// Gets or sets whether console output is enabled.
    /// Defaults to false - must be explicitly enabled when buildserver output is requested.
    /// </summary>
    public static bool IsConsoleEnabled { get; private set; }

    private string? cachedLogFilePath;
    private LogEventProperty? cachedLogFilePathProp;

    private static string logFilePath = string.Empty;

    /// <summary>
    /// The property name used to store the log file path.
    /// </summary>
    public const string LogFilePathPropertyName = "LogFilePath";

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        LogEventProperty logFilePathProp;

        if (cachedLogFilePathProp != null && logFilePath.Equals(cachedLogFilePath))
        {
            logFilePathProp = cachedLogFilePathProp;
        }
        else
        {
            cachedLogFilePath = logFilePath;
            cachedLogFilePathProp = logFilePathProp = propertyFactory.CreateProperty(LogFilePathPropertyName, logFilePath);
        }

        logEvent.AddPropertyIfAbsent(logFilePathProp);
    }

    /// <summary>
    /// Configures the logging enricher with the specified log file path and verbosity.
    /// </summary>
    /// <param name="logFile">The log file path, or null to disable file logging.</param>
    /// <param name="verbosity">The verbosity level.</param>
    /// <param name="enableConsoleOutput">Whether to enable console output. When false, console output is suppressed.</param>
    public static void Configure(string? logFile, Verbosity verbosity, bool enableConsoleOutput = true)
    {
        if (!string.IsNullOrWhiteSpace(logFile))
            logFilePath = logFile;
        LogLevelSwitch.MinimumLevel = GetLevelForVerbosity(verbosity);
        IsConsoleEnabled = enableConsoleOutput;
    }

    /// <summary>
    /// Configures the logging enricher with the specified log file path and log level.
    /// </summary>
    /// <param name="logFile">The log file path, or null to disable file logging.</param>
    /// <param name="logLevel">The Microsoft.Extensions.Logging log level.</param>
    /// <param name="enableConsoleOutput">Whether to enable console output. When false, console output is suppressed.</param>
    public static void Configure(string? logFile, LogLevel logLevel, bool enableConsoleOutput = true)
    {
        if (!string.IsNullOrWhiteSpace(logFile))
            logFilePath = logFile;
        LogLevelSwitch.MinimumLevel = GetLevelForLogLevel(logLevel);
        IsConsoleEnabled = enableConsoleOutput;
    }

    private static LogEventLevel GetLevelForVerbosity(Verbosity verbosity) => VerbosityMaps[verbosity];

    private static readonly Dictionary<Verbosity, LogEventLevel> VerbosityMaps = new()
    {
        { Verbosity.Verbose, LogEventLevel.Verbose },
        { Verbosity.Diagnostic, LogEventLevel.Debug },
        { Verbosity.Normal, LogEventLevel.Information },
        { Verbosity.Minimal, LogEventLevel.Warning },
        { Verbosity.Quiet, LogEventLevel.Error }
    };

    private static LogEventLevel GetLevelForLogLevel(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical or LogLevel.None => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };
}
