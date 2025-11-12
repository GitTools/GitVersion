using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace GitVersion.Logging;

public class LoggingModule
{
    public static IServiceCollection AddLogging(
        IServiceCollection services,
        Verbosity verbosity = Verbosity.Normal,
        bool addConsole = false,
        string? logFilePath = null,
        IFileSystem? fileSystem = null)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext();

        if (addConsole)
        {
            loggerConfiguration = AddConsoleLogger(loggerConfiguration, verbosity);
        }

        if (!string.IsNullOrWhiteSpace(logFilePath) && fileSystem != null)
        {
            loggerConfiguration = AddFileLogger(loggerConfiguration, fileSystem, logFilePath, verbosity);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
            builder.SetMinimumLevel(MapVerbosityToMicrosoftLogLevel(verbosity));
        });

        return services;
    }

    private static LoggerConfiguration AddConsoleLogger(LoggerConfiguration configuration, Verbosity verbosity)
    {
        var logLevel = MapVerbosityToSerilogLevel(verbosity);

        return configuration.WriteTo.Console(
            restrictedToMinimumLevel: logLevel,
            outputTemplate: "{Level:u} [{Timestamp:yy-MM-dd H:mm:ss:ff}] {Message:lj}{NewLine}{Exception}",
            theme: AnsiConsoleTheme.Code,
            applyThemeToRedirectedOutput: true
        );
    }

    private static LoggerConfiguration AddFileLogger(
        LoggerConfiguration configuration,
        IFileSystem fileSystem,
        string filePath,
        Verbosity verbosity)
    {
        fileSystem.NotNull();

        var logFile = fileSystem.FileInfo.New(FileSystemHelper.Path.GetFullPath(filePath));
        logFile.Directory?.Create();

        var logLevel = MapVerbosityToSerilogLevel(verbosity);

        return configuration.WriteTo.File(
            path: filePath,
            restrictedToMinimumLevel: logLevel,
            outputTemplate: "{Level:u} [{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message:lj}{NewLine}{Exception}",
            shared: true,
            flushToDiskInterval: TimeSpan.FromSeconds(1)
        );
    }

    private static LogEventLevel MapVerbosityToSerilogLevel(Verbosity verbosity) => verbosity switch
    {
        Verbosity.Quiet => LogEventLevel.Error,
        Verbosity.Minimal => LogEventLevel.Warning,
        Verbosity.Normal => LogEventLevel.Information,
        Verbosity.Verbose => LogEventLevel.Verbose,
        Verbosity.Diagnostic => LogEventLevel.Debug,
        _ => LogEventLevel.Information
    };

    private static Microsoft.Extensions.Logging.LogLevel MapVerbosityToMicrosoftLogLevel(Verbosity verbosity) => verbosity switch
    {
        Verbosity.Quiet => Microsoft.Extensions.Logging.LogLevel.Error,
        Verbosity.Minimal => Microsoft.Extensions.Logging.LogLevel.Warning,
        Verbosity.Normal => Microsoft.Extensions.Logging.LogLevel.Information,
        Verbosity.Verbose => Microsoft.Extensions.Logging.LogLevel.Debug,
        Verbosity.Diagnostic => Microsoft.Extensions.Logging.LogLevel.Trace,
        _ => Microsoft.Extensions.Logging.LogLevel.Information
    };
}
