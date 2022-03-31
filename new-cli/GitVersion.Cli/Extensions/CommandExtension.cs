using System.CommandLine;
using System.Reflection;

namespace GitVersion.Extensions;

public static class CommandExtension
{
    private const BindingFlags DeclaredOnly = BindingFlags.Public | BindingFlags.Instance;

    public static void AddOptions(this Command command, Type commandOptionsType)
    {
        var propertyInfos = commandOptionsType.GetProperties(DeclaredOnly);
        foreach (var propertyInfo in propertyInfos)
        {
            var optionAttribute = propertyInfo.GetCustomAttribute<OptionAttribute>();
            if (optionAttribute == null) continue;

            var option = new Option(optionAttribute.Aliases, optionAttribute.Description, propertyInfo.PropertyType)
            {
                IsRequired = optionAttribute.IsRequired,
            };
            command.AddOption(option);
        }
    }
}
