namespace GitVersion
{
    using System.IO;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public class ConfigSerialiser
    {
        public static Config Read(TextReader reader)
        {
            var deserializer = new Deserializer(null, new HyphenatedNamingConvention());
            var deserialize = deserializer.Deserialize<Config>(reader);
            if (deserialize == null)
            {
                return new Config();
            }
            return deserialize;
        }

        public static void Write(Config config, TextWriter writer)
        {
            var serializer = new Serializer(SerializationOptions.None, new HyphenatedNamingConvention());
            serializer.Serialize(writer, config);
        }
    }
}