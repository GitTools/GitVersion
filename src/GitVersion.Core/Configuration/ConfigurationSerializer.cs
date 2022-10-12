using GitVersion.Model.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion.Configuration;

public class ConfigurationSerializer
{
    public static GitVersionConfiguration Read(TextReader reader)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
        var configuration = deserializer.Deserialize<GitVersionConfiguration?>(reader);
        return configuration ?? new GitVersionConfiguration();
    }

    public static void Write(GitVersionConfiguration configuration, TextWriter writer)
    {
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
        serializer.Serialize(writer, configuration);
    }
}
