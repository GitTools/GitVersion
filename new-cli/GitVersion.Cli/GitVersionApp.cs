using System.CommandLine;
using GitVersion.Extensions;
using GitVersion.Generated;
using GitVersion.Infrastructure;
using Serilog.Events;

namespace GitVersion;

// ReSharper disable once ClassNeverInstantiated.Global
internal class GitVersionApp(RootCommandImpl rootCommand)
{
    private readonly RootCommandImpl rootCommand = rootCommand.NotNull();

    public Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var cliConfiguration = new CliConfiguration(rootCommand);
        var parseResult = cliConfiguration.Parse(args);

        var logFile = parseResult.GetValue<FileInfo?>(GitVersionSettings.LogFileOption);
        var verbosity = parseResult.GetValue<Verbosity?>(GitVersionSettings.VerbosityOption) ?? Verbosity.Normal;

        if (logFile?.FullName != null) LoggingEnricher.Path = logFile.FullName;
        LoggingEnricher.LogLevel.MinimumLevel = GetLevelForVerbosity(verbosity);

        return parseResult.InvokeAsync(cancellationToken);
    }

    // Note: there are 2 locations to watch for dotnet-suggest
    // - sentinel file: $env:TEMP\system-commandline-sentinel-files\ and
    // - registration file: $env:LOCALAPPDATA\.dotnet-suggest-registration.txt or $HOME/.dotnet-suggest-registration.txt

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
