using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;

namespace GitVersion.Configuration;

internal static class ConfigurationSerializer
{
    private static IDeserializer Deserializer => new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .WithTypeInspector(inspector => new JsonPropertyNameInspector(inspector))
        .Build();

    private static ISerializer Serializer => new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithTypeInspector(inspector => new JsonPropertyNameInspector(inspector))
        .WithNamingConvention(HyphenatedNamingConvention.Instance).Build();

    public static T Deserialize<T>(string input) => Deserializer.Deserialize<T>(input);

    public static string Serialize(object graph) => Serializer.Serialize(graph);

    public static IGitVersionConfiguration Read(TextReader reader)
    {
        var configuration = Deserializer.Deserialize<GitVersionConfiguration?>(reader);
        return configuration ?? new GitVersionConfiguration();
    }

    public static void Write(IGitVersionConfiguration configuration, TextWriter writer)
        => Serializer.Serialize(writer, configuration);
}

internal sealed class JsonPropertyNameInspector : TypeInspectorSkeleton
{
    private readonly ITypeInspector innerTypeDescriptor;

    public JsonPropertyNameInspector(ITypeInspector innerTypeDescriptor) => this.innerTypeDescriptor = innerTypeDescriptor;

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) =>
        innerTypeDescriptor.GetProperties(type, container)
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            .Select(p =>
            {
                var descriptor = new PropertyDescriptor(p);
                var member = p.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (member is { Name: { } })
                {
                    descriptor.Name = member.Name;
                }

                return (IPropertyDescriptor)descriptor;
            })
            .OrderBy(p => p.Order);
}
