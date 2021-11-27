using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using GitVersion.Command;
using GitVersion.Extensions;
using ICommand = GitVersion.Command.ICommand;

namespace GitVersion;

internal class GitVersionApp
{
    private readonly RootCommand rootCommand;

    public GitVersionApp(IEnumerable<ICommand> commandHandlers) =>
        rootCommand = MapCommands(commandHandlers);

    public Task<int> RunAsync(string[] args)
    {
        return new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .Build()
            .InvokeAsync(args);
    }

    private static RootCommand MapCommands(IEnumerable<ICommand> handlers)
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