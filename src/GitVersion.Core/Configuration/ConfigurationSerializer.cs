using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion.Configuration;

public static class ConfigurationSerializer
{
    private static IDeserializer Deserializer
        => new DeserializerBuilder().WithNamingConvention(HyphenatedNamingConvention.Instance).Build();

    public static T Deserialize<T>(string input) => Deserializer.Deserialize<T>(input);

    public static string Serialize(object graph) => Serializer.Serialize(graph);

    private static ISerializer Serializer
        => new SerializerBuilder().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithNamingConvention(HyphenatedNamingConvention.Instance).Build();

    public static GitVersionConfiguration Read(TextReader reader)
    {
        var configuration = Deserializer.Deserialize<GitVersionConfiguration?>(reader);
        return configuration ?? new GitVersionConfiguration();
    }

    public static void Write(GitVersionConfiguration configuration, TextWriter writer)
        => Serializer.Serialize(writer, configuration);
}
