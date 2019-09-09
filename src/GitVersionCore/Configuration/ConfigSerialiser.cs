using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion.Configuration
{
    public class ConfigSerialiser
    {
        public static Config Read(TextReader reader)
        {
            var deserializer = new DeserializerBuilder().WithNamingConvention(new HyphenatedNamingConvention()).Build();
            var deserialize = deserializer.Deserialize<Config>(reader);
            if (deserialize == null)
            {
                return new Config();
            }
            return deserialize;
        }

        public static void Write(Config config, TextWriter writer)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(new HyphenatedNamingConvention()).Build();
            serializer.Serialize(writer, config);
        }
    }
}
