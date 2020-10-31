using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading.Tasks;
using GitVersion.Command;
using ICommandHandler = GitVersion.Command.ICommandHandler;

namespace GitVersion.Cli
{
    internal class GitVersionApp : RootCommand
    {
        public GitVersionApp(IEnumerable<IRootCommandHandler> commandHandlers)
        {
            foreach (var commandHandler in commandHandlers)
            {
                var command = CreateCommand(commandHandler);
                if (command != null) AddCommand(command);
            }
        }

        public Task<int> RunAsync(string[] args)
        {
            return new CommandLineBuilder(this)
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }

        private static System.CommandLine.Command? CreateCommand(ICommandHandler commandHandler)
        {
            const BindingFlags declaredOnly = BindingFlags.Public | BindingFlags.Instance;

            var handlerType = commandHandler.GetType();
            if (handlerType.BaseType is null)
                return null;

            var commandOptionsType = handlerType.BaseType.GenericTypeArguments[0];
            var commandAttribute = commandOptionsType.GetCustomAttribute<CommandAttribute>();

            if (commandAttribute == null) 
                return null;

            var command = new System.CommandLine.Command(commandAttribute.Name, commandAttribute.Description);
            var propertyInfos = commandOptionsType.GetProperties(declaredOnly);
            foreach (var propertyInfo in propertyInfos)
            {
                var optionAttribute = propertyInfo.GetCustomAttribute<OptionAttribute>();
                if (optionAttribute == null) continue;

                var option = new Option(optionAttribute.Aliases, optionAttribute.Description)
                {
                    IsRequired = optionAttribute.IsRequired,
                    Argument = new Argument { ArgumentType = propertyInfo.PropertyType }
                };
                command.AddOption(option);
            }

            var handlerMethod =
                handlerType.GetMethod(nameof(commandHandler.InvokeAsync), new[] { commandOptionsType });
            command.Handler = CommandHandler.Create(handlerMethod ?? throw new InvalidOperationException(),
                commandHandler);

            foreach (var subCommandHandler in commandHandler.SubCommands())
            {
                var subCommand = CreateCommand(subCommandHandler);
                if (subCommand != null) command.AddCommand(subCommand);
            }

            return command;
        }
    }
}