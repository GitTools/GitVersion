using GitVersion.Model.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion.Configuration;

public class ConfigSerializer
{
    public static Config Read(TextReader reader)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
        var config = deserializer.Deserialize<Config?>(reader);
        return config ?? new Config();
    }

    public static void Write(Config config, TextWriter writer)
    {
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
        serializer.Serialize(writer, config);
    }
}
