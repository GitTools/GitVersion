namespace GitVersion
{
    using System.IO;
    using System.Text;
    using GitVersion.Helpers;

    public class ConfigurationProvider
    {
        public static Config Provide(string workingDirectory, IFileSystem fileSystem)
        {
            var configFilePath = GetConfigFilePath(workingDirectory);

            if (fileSystem.Exists(configFilePath))
            {
                var readAllText = fileSystem.ReadAllText(configFilePath);
                LegacyConfigNotifier.Notify(new StringReader(readAllText));

                return ConfigSerialiser.Read(new StringReader(readAllText));
            }

            return new Config();
        }

        public static string GetEffectiveConfigAsString(string gitDirectory, IFileSystem fileSystem)
        {
            var config = Provide(gitDirectory, fileSystem);
            var stringBuilder = new StringBuilder();
            using (var stream = new StringWriter(stringBuilder))
            {
                ConfigSerialiser.Write(config, stream);
                stream.Flush();
            }
            return stringBuilder.ToString();
        }

        public static void WriteSample(string workingDirectory, IFileSystem fileSystem)
        {
            var configFilePath = GetConfigFilePath(workingDirectory);

            if (!fileSystem.Exists(configFilePath))
            {
                using (var stream = fileSystem.OpenWrite(configFilePath))
                using (var writer = new StreamWriter(stream))
                {
                    ConfigSerialiser.WriteSample(writer);
                }
            }
            else
            {
                Logger.WriteError("Cannot write sample, GitVersionConfig.yaml already exists");
            }
        }

        static string GetConfigFilePath(string workingDirectory)
        {
            return Path.Combine(Directory.GetParent(workingDirectory).FullName, "GitVersionConfig.yaml");
        }
    }
}