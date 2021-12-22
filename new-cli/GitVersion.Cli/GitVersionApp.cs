using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Reflection;
using GitVersion.Command;
using GitVersion.Extensions;
using GitVersion.Infrastructure;
using Serilog.Events;
using ICommand = GitVersion.Command.ICommand;

namespace GitVersion;

internal class GitVersionApp
{
    private readonly RootCommand rootCommand;

    public GitVersionApp(IEnumerable<ICommand> commandHandlers) => rootCommand = CreateCommandsHierarchy(commandHandlers);

    public Task<int> RunAsync(string[] args)
    {
        return new CommandLineBuilder(rootCommand)
            .AddMiddleware(async (context, next) =>
            {
                EnrichLogger(context);
                await next(context);
            })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);
    }

    private static void EnrichLogger(InvocationContext context)
    {
        Option? GetOption(string alias)
        {
            foreach (var symbolResult in context.ParseResult.CommandResult.Children)
            {
                if (symbolResult.Symbol is Option id && id.HasAlias(alias))
                {
                    return id;
                }
            }
            return null;
        }
        
        T? GetOptionValue<T>(string alias)
        {
            var option = GetOption(alias);
            return option != null ? context.ParseResult.GetValueForOption<T>(option) : default;
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

    private static RootCommand CreateCommandsHierarchy(IEnumerable<ICommand> handlers)
    {
        var commandsMap = new Dictionary<Type, Infrastructure.Command>();
        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType();
            var commandSettingsType = handlerType.BaseType?.GenericTypeArguments[0];
            if (commandSettingsType != null)
            {
                var commandAttribute = handlerType.GetCustomAttribute<CommandAttribute>();
                if (commandAttribute != null)
                {
                    var command = new Infrastructure.Command(commandAttribute.Name, commandAttribute.Description)
                    {
                        Parent = commandAttribute.Parent
                    };
                    command.AddOptions(commandSettingsType);

                    var handlerMethod = handlerType.GetMethod(nameof(ICommand.InvokeAsync));

                    command.Handler = CommandHandler.Create(handlerMethod!, handler);
                    // command.SetHandler(handlerDelegate);

                    commandsMap.Add(commandSettingsType, command);
                }
            }
        }

        var parentGroups = commandsMap.GroupBy(x => x.Value.Parent, x => x.Key).ToList();

        var rootCommand = new RootCommand();
        foreach (var parentGroup in parentGroups)
        {
            System.CommandLine.Command command = parentGroup.Key is null
                ? rootCommand
                : commandsMap[parentGroup.Key];

            foreach (var child in parentGroup)
            {
                command.AddCommand(commandsMap[child]);
            }
        }

        return rootCommand;
    }
}