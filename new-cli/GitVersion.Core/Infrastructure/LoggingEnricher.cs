using Serilog.Core;
using Serilog.Events;

namespace GitVersion.Infrastructure;

public class LoggingEnricher : ILogEventEnricher
{
    public static readonly LoggingLevelSwitch LogLevel = new();
    private string? cachedLogFilePath;
    private LogEventProperty? cachedLogFilePathProp;

    // this path and level will be set by the LogInterceptor.cs after parsing the settings
    private static string path = string.Empty;

    public const string LogFilePathPropertyName = "LogFilePath";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // the settings might not have a path, or we might not be within a command, in which case
        // we won't have the setting, so a default value for the log file will be required
        LogEventProperty logFilePathProp;

        if (this.cachedLogFilePathProp != null && path.Equals(this.cachedLogFilePath))
        {
            // The Path hasn't changed, so let's use the cached property
            logFilePathProp = this.cachedLogFilePathProp;
        }
        else
        {
            // We've got a new path for the log. Let's create a new property
            // and cache it for future log events to use
            this.cachedLogFilePath = path;
            this.cachedLogFilePathProp = logFilePathProp = propertyFactory.CreateProperty(LogFilePathPropertyName, path);
        }

        logEvent.AddPropertyIfAbsent(logFilePathProp);
    }

    public static void Configure(string? logFile, Verbosity verbosity)
    {
        if (!string.IsNullOrWhiteSpace(logFile)) path = logFile;
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
