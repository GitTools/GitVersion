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
        var config = deserializer.Deserialize<Configuration?>(reader);
        return config ?? new Configuration();
    }

    public static void Write(Model.Configurations.Configuration config, TextWriter writer)
    {
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
        serializer.Serialize(writer, config);
    }
}
