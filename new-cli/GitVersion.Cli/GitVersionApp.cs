using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using GitVersion.Generated;
using GitVersion.Infrastructure;
using Serilog.Events;

namespace GitVersion;

internal class GitVersionApp
{
    private readonly RootCommandImpl rootCommand;
    public GitVersionApp(RootCommandImpl rootCommand) => this.rootCommand = rootCommand;

    public Task<int> RunAsync(string[] args) =>
        new CommandLineBuilder(rootCommand)
            .AddMiddleware(async (context, next) =>
            {
                EnrichLogger(context);
                await next(context);
            })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);

    private static void EnrichLogger(InvocationContext context)
    {
        Option<T>? GetOption<T>(string alias)
        {
            foreach (var symbolResult in context.ParseResult.CommandResult.Children)
            {
                if (symbolResult.Symbol is Option<T> id && id.HasAlias(alias))
                    return id;
            }
            return null;
        }

        T? GetOptionValue<T>(string alias)
        {
            var option = GetOption<T>(alias);
            return option != null ? context.ParseResult.GetValueForOption(option) : default;
        }

        var logFile = GetOptionValue<FileInfo>(GitVersionSettings.LogFileOptionAlias1);
        var verbosity = GetOptionValue<Verbosity?>(GitVersionSettings.VerbosityOption) ?? Verbosity.Normal;

        LoggingEnricher.Path = logFile?.FullName ?? "log.txt";
        LoggingEnricher.LogLevel.MinimumLevel = GetLevelForVerbosity(verbosity);
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
