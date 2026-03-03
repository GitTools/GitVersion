using System.Globalization;
using GitVersion.Logging;
using Serilog;

namespace GitVersion.Extensions;

/// <summary>Extension methods on <see cref="IServiceCollection"/> and <see cref="IServiceProvider"/> for GitVersion module registration.</summary>
#pragma warning disable S2325, S1144
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        /// <summary>Registers all services declared by <paramref name="gitVersionModule"/> into <paramref name="serviceCollection"/>.</summary>
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
            serviceCollection.AddSerilog((services, loggerConfig) =>
            {
                var options = services.GetRequiredService<IOptions<GitVersionOptions>>().Value;

                ConfigureLogger(loggerConfig, options);
            });
            return serviceCollection;
        }
    }

    extension(IServiceProvider serviceProvider)
    {
        /// <summary>Returns the registered <typeparamref name="TService"/> whose concrete type is exactly <typeparamref name="TType"/>.</summary>
        public TService GetServiceForType<TService, TType>() =>
            serviceProvider.GetServices<TService>().Single(t => t?.GetType() == typeof(TType));
    }

    private static void ConfigureLogger(LoggerConfiguration loggerConfig, GitVersionOptions gitVersionOptions)
    {
        const string outputTemplate = "{Level:u4} [{Timestamp:yy-MM-dd HH:mm:ss:ff}] {Indent}{Message:lj}{NewLine}{Exception}";
        const string logDestination = "console";
        var formatProvider = CultureInfo.InvariantCulture;

        loggerConfig
            .Enrich.With<SensitiveDataEnricher>()
            .Enrich.With<IndentationEnricher>();

        if (ShouldLogToConsole())
        {
            loggerConfig.WriteTo.Console(outputTemplate: outputTemplate, formatProvider: formatProvider);
        }

        if (ShouldLogToFile())
        {
            loggerConfig.WriteTo.File(gitVersionOptions.LogFilePath!, outputTemplate: outputTemplate, formatProvider: formatProvider);
        }

        return;

        bool ShouldLogToConsole() =>
            gitVersionOptions.Output.Contains(OutputType.BuildServer)
            || logDestination.IsEquivalentTo(gitVersionOptions.LogFilePath);

        bool ShouldLogToFile() =>
            !(string.IsNullOrEmpty(gitVersionOptions.LogFilePath)
              || gitVersionOptions.LogFilePath.IsEquivalentTo(logDestination));
    }
}
#pragma warning restore S2325, S1144
