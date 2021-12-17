using System.CommandLine;
using System.CommandLine.Parsing;
using GitVersion.Command;
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

    public IContainerRegistrar AddLogging(string[] args)
    {
        var logger = GetLogger(args);
        services.AddLogging(builder => builder.AddSerilog(logger, dispose: true));
        services.AddSingleton<ILogger>(provider => new Logger(provider.GetService<ILogger<Logger>>()!));
        return this;
    }

    public IContainer Build() => new Container(services.BuildServiceProvider());

    private static Serilog.Core.Logger GetLogger(string[] args)
    {
        // We cannot use the logFile path when the logger was already created and registered in DI container
        // so we perform a pre-parse of the arguments to fetch the logFile so that we can create the logger and
        // register in the DI container
        var aliases = new[] { GitVersionSettings.LogFileOptionAlias1, GitVersionSettings.LogFileOptionAlias2 };
        var option = new Option(aliases, argumentType: typeof(FileInfo));
        var logFile = new Parser(option).Parse(args).ValueForOption<FileInfo>(option);

        var configuration = new LoggerConfiguration()
            .WriteTo.Console();

        if (logFile != null)
        {
            var path = logFile.FullName;
            configuration = configuration
                .WriteTo.File(path);
        }

        return configuration.CreateLogger();
    }
}