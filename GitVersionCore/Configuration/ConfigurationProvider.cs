namespace GitVersion
{
    using System.IO;
    using System.Text;
    using GitVersion.Configuration.Init.Wizard;
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

        static string GetConfigFilePath(string workingDirectory)
        {
            return Path.Combine(workingDirectory, "GitVersionConfig.yaml");
        }

        public static void Init(string workingDirectory, IFileSystem fileSystem)
        {
            var configFilePath = GetConfigFilePath(workingDirectory);
            var config = new ConfigInitWizard().Run(Provide(workingDirectory, fileSystem));
            if (config == null) return;

            using (var stream = fileSystem.OpenWrite(configFilePath))
            using (var writer = new StreamWriter(stream))
            {
                Logger.WriteInfo("Saving config file");
                ConfigSerialiser.Write(config, writer);
                stream.Flush();
            }
        }
    }
}