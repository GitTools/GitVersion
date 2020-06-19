using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading.Tasks;
using Core;
using ICommandHandler = Core.ICommandHandler;

namespace Cli
{
    internal class GitVersionApp : RootCommand
    {
        public GitVersionApp(IEnumerable<ICommandHandler> commandHandlers)
        {
            foreach (var commandHandler in commandHandlers)
            {
                var command = GetCommand(commandHandler);
                AddCommand(command);
            }
        }

        public Task<int> RunAsync(string[] args)
        {
            return new CommandLineBuilder(this)
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }

        private static Command GetCommand(ICommandHandler commandHandler)
        {
            const BindingFlags declaredOnly = BindingFlags.Public | BindingFlags.Instance;

            var handlerType = commandHandler.GetType();
            var commandType = handlerType.BaseType?.GenericTypeArguments[0];
            var commandAttribute = commandType?.GetCustomAttribute<CommandAttribute>();

            if (commandAttribute == null) return null;

            var command = new Command(commandAttribute.Name, commandAttribute.Description);
            var propertyInfos = commandType.GetProperties(declaredOnly);
            foreach (var propertyInfo in propertyInfos)
            {
                var optionAttribute = propertyInfo.GetCustomAttribute<OptionAttribute>();
                if (optionAttribute == null) continue;

                var option = new Option(optionAttribute.Aliases, optionAttribute.Description)
                {
                    Required = optionAttribute.Required,
                    Argument = new Argument { ArgumentType = propertyInfo.PropertyType }
                };
                command.AddOption(option);
            }
            
            var handlerMethod = handlerType.GetMethod(nameof(CommandHandler<int>.InvokeAsync), new [] { commandType });
            command.Handler = CommandHandler.Create(handlerMethod, commandHandler);

            foreach (var subCommandHandler in commandHandler.GetSubCommands())
            {
                var subCommand = GetCommand(subCommandHandler);
                command.AddCommand(subCommand);
            }

            return command;
        }
    }
}