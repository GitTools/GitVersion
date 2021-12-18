using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace GitVersion.Infrastructure;

public class ContainerRegistrar : IContainerRegistrar
{
    private readonly ServiceCollection services = new();

    public IContainerRegistrar AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        services.AddSingleton<TService, TImplementation>();
        return this;
    }

    public IContainerRegistrar AddSingleton<TService>()
        where TService : class
        => AddSingleton<TService, TService>();

    public IContainerRegistrar AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        services.AddTransient<TService, TImplementation>();
        return this;
    }

    public IContainerRegistrar AddTransient<TService>()
        where TService : class
        => AddTransient<TService, TService>();

    public IContainerRegistrar AddLogging()
    {
        services.AddLogging(builder =>
        {
            var logger = CreateLogger();
            builder.AddSerilog(logger, dispose: true);
        });
        services.AddSingleton<ILogger>(provider => new Logger(provider.GetService<ILogger<Logger>>()!));
        return this;
    }

    public IContainer Build() => new Container(services.BuildServiceProvider());
    
    private static Serilog.Core.Logger CreateLogger()
    {
        var logger = new LoggerConfiguration()
            // log level will be dynamically be controlled by our log interceptor upon running
            .MinimumLevel.ControlledBy(LoggingEnricher.LogLevel)
            // the log enricher will add a new property with the log file path from the settings
            // that we can use to set the path dynamically
            .Enrich.With<LoggingEnricher>()
            // serilog.sinks.map will defer the configuration of the sink to be on demand
            // allowing us to look at the properties set by the enricher to set the path appropriately
            .WriteTo.Console()
            .WriteTo.Map(LoggingEnricher.LogFilePathPropertyName, (logFilePath, wt) => wt.File(logFilePath), 1)
            .CreateLogger();
        return logger;
    }
}