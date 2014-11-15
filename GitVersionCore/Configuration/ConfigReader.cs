using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion.Configuration
{
    public class ConfigReader
    {
        public static Config Read(TextReader reader)
        {
            var deserializer = new Deserializer(null, new CamelCaseNamingConvention());
            var deserialize = deserializer.Deserialize<Config>(reader);
            if (deserialize == null)
            {
                return new Config();
            }
            return deserialize;
        }
    }
}