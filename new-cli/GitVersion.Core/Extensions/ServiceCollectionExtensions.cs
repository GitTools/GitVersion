using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GitVersion.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterModules(this IServiceCollection services, IEnumerable<IGitVersionModule> gitVersionModules)
        => gitVersionModules.Aggregate(services, (current, gitVersionModule) => gitVersionModule.RegisterTypes(current));

    public static IServiceCollection RegisterLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            var logger = CreateLogger();
            builder.AddSerilog(logger, dispose: true);
        });

        return services;
    }

    private static Serilog.Core.Logger CreateLogger()
    {
        var logger = new LoggerConfiguration()
            // log level will dynamically be controlled by our log interceptor upon running
            .MinimumLevel.ControlledBy(LoggingEnricher.LogLevel)
            // the log enricher will add a new property with the log file path from the settings
            // that we can use to set the path dynamically
            .Enrich.With<LoggingEnricher>()
            // serilog.sinks.map will defer the configuration of the sink to be on demand,
            // allowing us to look at the properties set by the enricher to set the path appropriately
            .WriteTo.Console()
            .WriteTo.Map(LoggingEnricher.LogFilePathPropertyName, (logFilePath, sinkConfiguration) =>
            {
                if (!string.IsNullOrEmpty(logFilePath)) sinkConfiguration.File(logFilePath);
            }, 1)
            .CreateLogger();
        return logger;
    }
}
