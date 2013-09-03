using System.Linq;
using Mono.Cecil;

public static class Extensions
{
    public static string InfoVersion(this ModuleDefinition moduleDefinition)
    {
        var customAttribute = moduleDefinition.Assembly.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == "AssemblyInformationalVersionAttribute");

        if (customAttribute != null)
        {
            return (string)customAttribute.ConstructorArguments[0].Value; 
        }
        return null;
    }
}