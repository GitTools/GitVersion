using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using Core;
using ICommandHandler = Core.ICommandHandler;

namespace Cli
{
    public static class CommandExtensions
    {
        public static Command GetCommand(this ICommandHandler commandHandler)
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
            
            var handlerMethod = handlerType.GetMethod(nameof(CommandHandler<int>.InvokeAsync), new []{ commandType });
            command.Handler = CommandHandler.Create(handlerMethod, commandHandler);

            foreach (var subCommandHandler in commandHandler.GetSubCommands())
            {
                var subCommand = subCommandHandler.GetCommand();
                command.AddCommand(subCommand);
            }

            return command;
        }
    }
}