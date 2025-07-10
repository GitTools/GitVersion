using Serilog.Core;
using Serilog.Events;

namespace GitVersion.Infrastructure;

public class LoggingEnricher : ILogEventEnricher
{
    public static readonly LoggingLevelSwitch LogLevel = new();
    private string? _cachedLogFilePath;
    private LogEventProperty? _cachedLogFilePathProp;

    // this path and level will be set by the LogInterceptor.cs after parsing the settings
    private static string _path = string.Empty;

    public const string LogFilePathPropertyName = "LogFilePath";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propFactory)
    {
        // the settings might not have a path, or we might not be within a command, in which case
        // we won't have the setting, so a default value for the log file will be required
        LogEventProperty logFilePathProp;

        if (_cachedLogFilePathProp != null && _path.Equals(_cachedLogFilePath))
        {
            // The Path hasn't changed, so let's use the cached property
            logFilePathProp = _cachedLogFilePathProp;
        }
        else
        {
            // We've got a new path for the log. Let's create a new property
            // and cache it for future log events to use
            _cachedLogFilePath = _path;
            _cachedLogFilePathProp = logFilePathProp = propFactory.CreateProperty(LogFilePathPropertyName, _path);
        }

        logEvent.AddPropertyIfAbsent(logFilePathProp);
    }

    public static void Configure(string? logFile, Verbosity verbosity)
    {
        if (!string.IsNullOrWhiteSpace(logFile)) _path = logFile;
        LogLevel.MinimumLevel = GetLevelForVerbosity(verbosity);
    }

    private static LogEventLevel GetLevelForVerbosity(Verbosity verbosity) => VerbosityMaps[verbosity];

    private static readonly Dictionary<Verbosity, LogEventLevel> VerbosityMaps = new()
    {
        { Verbosity.Verbose, LogEventLevel.Verbose },
        { Verbosity.Diagnostic, LogEventLevel.Debug },
        { Verbosity.Normal, LogEventLevel.Information },
        { Verbosity.Minimal, LogEventLevel.Warning },
        { Verbosity.Quiet, LogEventLevel.Error },
    };
}
