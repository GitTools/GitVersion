namespace GitVersion
{
    using System;
    using System.Linq;
    using System.Reflection;

    class VersionWriter
    {
        public static void Write(Assembly assembly)
        {
            WriteTo(assembly, Console.WriteLine);
        }

        public static void WriteTo(Assembly assembly, Action<string> writeAction)
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
