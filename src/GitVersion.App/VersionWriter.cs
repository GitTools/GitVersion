using GitVersion.Extensions;

namespace GitVersion;

internal class VersionWriter(IConsole console) : IVersionWriter
{
    private readonly IConsole console = console.NotNull();

    public void Write(Assembly assembly) => WriteTo(assembly, this.console.WriteLine);

    public void WriteTo(Assembly assembly, Action<string?> writeAction)
    {
        var version = GetAssemblyVersion(assembly);
        writeAction(version);
    }

    private static string? GetAssemblyVersion(Assembly assembly)
    {
        if (assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .FirstOrDefault() is AssemblyInformationalVersionAttribute attribute)
        {
            return attribute.InformationalVersion;
        }

        return assembly.GetName().Version?.ToString();
    }
}
