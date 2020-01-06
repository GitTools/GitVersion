using System;
using System.Linq;
using System.Reflection;

namespace GitVersion
{
    public class VersionWriter : IVersionWriter
    {
        public void Write(Assembly assembly)
        {
            WriteTo(assembly, Console.WriteLine);
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
