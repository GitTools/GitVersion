using GitVersion.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = GitVersion.Infrastructure.ILogger;

namespace GitVersion.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterModules(this IServiceCollection services, IEnumerable<IGitVersionModule> gitVersionModules)
        => gitVersionModules.Aggregate(services, (current, gitVersionModule) => current.RegisterModule(gitVersionModule));

    public static IServiceCollection RegisterModule(this IServiceCollection services, IGitVersionModule gitVersionModule)
    {
        gitVersionModule.RegisterTypes(services);
        return services;
    }

    public static IServiceCollection RegisterLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            var logger = CreateLogger();
            builder.AddSerilog(logger, dispose: true);
        });
        services.AddSingleton<ILogger>(provider => new Logger(provider.GetRequiredService<ILogger<Logger>>()));
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
            .WriteTo.Map(LoggingEnricher.LogFilePathPropertyName, (logFilePath, wt) =>
            {
                if (!string.IsNullOrEmpty(logFilePath)) wt.File(logFilePath);
            }, 1)
            .CreateLogger();
        return logger;
    }
}
