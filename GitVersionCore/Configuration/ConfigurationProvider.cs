namespace GitVersion
{
    using System.IO;

    public class ConfigurationProvider
    {
        public static Config Provide(string gitDirectory)
        {
            var configFilePath = GetConfigFilePath(gitDirectory);
            if (File.Exists(configFilePath))
            {
                using (var reader = File.OpenText(configFilePath))
                {
                    return ConfigReader.Read(reader);
                }
            }

            return new Config();
        }

        public static void WriteSample(string gitDirectory)
        {
            var configFilePath = GetConfigFilePath(gitDirectory);

            if (!File.Exists(configFilePath))
            {
                using (var stream = File.OpenWrite(configFilePath))
                using (var writer = new StreamWriter(stream))
                {
                    ConfigReader.WriteSample(writer);
                }
            }
            // TODO else write warning?
        }

        static string GetConfigFilePath(string gitDirectory)
        {
            return Path.Combine(Directory.GetParent(gitDirectory).FullName, "GitVersionConfig.yaml");
        }
    }
}