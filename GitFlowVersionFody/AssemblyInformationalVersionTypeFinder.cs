using System.Linq;
using Mono.Cecil;

public static class AssemblyInformationalVersionTypeFinder
{

    public static TypeDefinition GetAssemblyInformationalVersionType(this ModuleDefinition moduleDefinition)
    {
        var msCoreLib = moduleDefinition.AssemblyResolver.Resolve("mscorlib");
        var msCoreAttribute = msCoreLib.MainModule.Types.FirstOrDefault(x => x.Name == "AssemblyInformationalVersionAttribute");
        if (msCoreAttribute != null)
        {
            return msCoreAttribute;
        }
        var systemRuntime = moduleDefinition.AssemblyResolver.Resolve("System.Runtime");
        return systemRuntime.MainModule.Types.First(x => x.Name == "AssemblyInformationalVersionAttribute");
    }
}