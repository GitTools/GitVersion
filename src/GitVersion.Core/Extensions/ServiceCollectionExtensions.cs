using System.Globalization;
using GitVersion.Logging;
using Serilog;

namespace GitVersion.Extensions;

#pragma warning disable S2325, S1144
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection AddModule(IGitVersionModule gitVersionModule)
        {
            gitVersionModule.RegisterTypes(serviceCollection);
            return serviceCollection;
        }

        /// <summary>
        /// Registers Serilog-based logging with console and optional file output.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        internal IServiceCollection AddGitVersionLogging()
        {
            serviceCollection.AddLogging(builder =>
            {
                // Clear default providers to ensure only Serilog is used
                builder.ClearProviders();
                var logger = CreateLogger();
                builder.AddSerilog(logger, dispose: true);
            });

            return serviceCollection;
        }
    }

    extension(IServiceProvider serviceProvider)
    {
        public TService GetServiceForType<TService, TType>() =>
            serviceProvider.GetServices<TService>().Single(t => t?.GetType() == typeof(TType));
    }

    private static Serilog.Core.Logger CreateLogger()
    {
        const string outputTemplate = "{Level:u4} [{Timestamp:yy-MM-dd HH:mm:ss:ff}] {Message:lj}{NewLine}{Exception}";
        var formatProvider = CultureInfo.InvariantCulture;
        var logger = new LoggerConfiguration()
            // Log level is dynamically controlled by LoggingEnricher.LogLevelSwitch
            .MinimumLevel.ControlledBy(LoggingEnricher.LogLevelSwitch)
            // Add the logging enricher for a dynamic log file path
            .Enrich.With<LoggingEnricher>()
            // Add sensitive data masking for URL passwords
            .Enrich.With<SensitiveDataEnricher>()
            // Console output with timestamp - controlled by IsConsoleEnabled flag
            .WriteTo.Conditional(
                _ => LoggingEnricher.IsConsoleEnabled,
                wt => wt.Console(
                    outputTemplate: outputTemplate,
                    formatProvider: formatProvider))
            // Dynamic file output using Serilog.Sinks.Map
            // Note: "console" is a special value that enables console output instead of file logging
            .WriteTo.Map(LoggingEnricher.LogFilePathPropertyName, (logFilePath, sinkConfiguration) =>
            {
                if (!string.IsNullOrEmpty(logFilePath) && !string.Equals(logFilePath, "console", StringComparison.OrdinalIgnoreCase))
                {
                    sinkConfiguration.File(
                        logFilePath,
                        outputTemplate: outputTemplate,
                        formatProvider: formatProvider);
                }
            }, 1)
            .CreateLogger();
        return logger;
    }
}
#pragma warning restore S2325, S1144
