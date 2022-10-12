using GitVersion.Model.Configurations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion.Configurations;

public class ConfigSerializer
{
    public static Model.Configurations.Configuration Read(TextReader reader)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
        var configuration = deserializer.Deserialize<Configuration?>(reader);
        return configuration ?? new Configuration();
    }

    public static void Write(Configuration configuration, TextWriter writer)
    {
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
        serializer.Serialize(writer, configuration);
    }
}
