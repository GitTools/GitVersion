using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GitVersion.Cli.Extensions;
using GitVersion.Cli.Infrastructure;
using GitVersion.Command;
using ICommandHandler = GitVersion.Command.ICommandHandler;

namespace GitVersion.Cli
{
    internal class GitVersionApp
    {
        private readonly RootCommand rootCommand;

        public GitVersionApp(IEnumerable<ICommandHandler> commandHandlers) => rootCommand = MapCommands(commandHandlers);

        public Task<int> RunAsync(string[] args)
        {
            return new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }
        
        private static RootCommand MapCommands(IEnumerable<ICommandHandler> handlers)
        {
            var commandsMap = new Dictionary<Type, GitVersionCommand>();
            foreach (var handler in handlers)
            {
                var handlerType = handler?.GetType();
                var commandOptionsType = handlerType?.BaseType?.GenericTypeArguments[0];
                if (commandOptionsType != null)
                {
                    var commandAttribute = commandOptionsType.GetCustomAttribute<CommandAttribute>();
                    if (commandAttribute != null)
                    {
                        var command = new GitVersionCommand(commandAttribute.Name, commandAttribute.Description)
                        {
                            Parent = commandAttribute.Parent
                        };
                        command.AddOptions(commandOptionsType);

                        var handlerMethod = handlerType?.GetMethod("InvokeAsync");
                        command.Handler = CommandHandler.Create(handlerMethod!, handler);

                        commandsMap.Add(commandOptionsType, command);
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
}