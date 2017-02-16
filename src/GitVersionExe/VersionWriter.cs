namespace GitVersion
{
    using System;
    using System.Linq;
    using System.Reflection;

    class VersionWriter
    {
        /// <summary>
        /// Gets the xss version
        /// </summary>
        private static Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        /// <summary>
        /// Gets the AssemblyInformationalVersion
        /// </summary>
        private static string InformationalVersion
        {
            get
            {
                var attribute = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                    .FirstOrDefault() as AssemblyInformationalVersionAttribute;

                if (attribute != null)
                {
                    return attribute.InformationalVersion;
                }

                return Version.ToString();
            }
        }

        public static void Write()
        {
            WriteTo(Console.WriteLine);
        }

        public static void WriteTo(Action<string> writeAction)
        {
            writeAction(InformationalVersion);
        }
    }
}
