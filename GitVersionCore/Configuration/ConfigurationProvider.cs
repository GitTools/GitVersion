namespace GitVersion
{
    using System.IO;

    public class ConfigurationProvider
    {
        public static Config Provide(string gitDirectory)
        {
            var configFilePath = Path.Combine(Directory.GetParent(gitDirectory).FullName, "GitVersionConfig.yaml");
            if (File.Exists(configFilePath))
            {
                using (var reader = File.OpenText(configFilePath))
                {
                    return ConfigReader.Read(reader);
                }
            }

            return new Config();
        }
    }
}