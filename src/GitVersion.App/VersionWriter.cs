using System;
using System.Linq;
using System.Reflection;
using GitVersion.Logging;

namespace GitVersion
{
    public class VersionWriter : IVersionWriter
    {
        private readonly IConsole console;

        public VersionWriter(IConsole console)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
        }
        public void Write(Assembly assembly)
        {
            WriteTo(assembly, console.WriteLine);
        }

        public void WriteTo(Assembly assembly, Action<string> writeAction)
        {
            var version = GetAssemblyVersion(assembly);
            writeAction(version);
        }

        private static string GetAssemblyVersion(Assembly assembly)
        {
            if (assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .FirstOrDefault() is AssemblyInformationalVersionAttribute attribute)
            {
                return attribute.InformationalVersion;
            }

            return assembly.GetName().Version.ToString();
        }
    }
}
