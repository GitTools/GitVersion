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

    public static void Configure(GitVersionOptions gitVersionOptions)
    {
        var enableConsoleOutput = gitVersionOptions.Output.Contains(OutputType.BuildServer)
                                  || string.Equals(gitVersionOptions.LogFilePath, "console", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(gitVersionOptions.LogFilePath))
        {
            logFilePath = gitVersionOptions.LogFilePath;
        }
        LogLevelSwitch.MinimumLevel = GetLevelForVerbosity(gitVersionOptions.Verbosity);
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
}
