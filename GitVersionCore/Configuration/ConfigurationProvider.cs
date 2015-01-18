namespace GitVersion
{
    using System.IO;
    using System.Text.RegularExpressions;
    using GitVersion.Helpers;

    public class ConfigurationProvider
    {
        static Regex oldAssemblyVersioningScheme = new Regex("assemblyVersioningScheme", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Config Provide(string gitDirectory, IFileSystem fileSystem)
        {
            var configFilePath = GetConfigFilePath(gitDirectory);

            if (fileSystem.Exists(configFilePath))
            {
                var readAllText = fileSystem.ReadAllText(configFilePath);
                LegacyConfigNotifier.Notify(new StringReader(readAllText));

                return ConfigReader.Read(new StringReader(readAllText));
            }

            return new Config();
        }

        public static void WriteSample(string gitDirectory, IFileSystem fileSystem)
        {
            var configFilePath = GetConfigFilePath(gitDirectory);

            if (!fileSystem.Exists(configFilePath))
            {
                using (var stream = fileSystem.OpenWrite(configFilePath))
                using (var writer = new StreamWriter(stream))
                {
                    ConfigReader.WriteSample(writer);
                }
            }
            else
            {
                Logger.WriteError("Cannot write sample, GitVersionConfig.yaml already exists");
            }
        }

        static string GetConfigFilePath(string gitDirectory)
        {
            return Path.Combine(Directory.GetParent(gitDirectory).FullName, "GitVersionConfig.yaml");
        }
    }
}