using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;

namespace GitVersion.Configuration;

internal class ConfigurationSerializer : IConfigurationSerializer
{
    private static IDeserializer Deserializer => new DeserializerBuilder()
        .WithNamingConvention(HyphenatedNamingConvention.Instance)
        .WithTypeConverter(VersionStrategiesConverter.Instance)
        .WithTypeInspector(inspector => new JsonPropertyNameInspector(inspector))
        .Build();

    private static ISerializer Serializer => new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithTypeInspector(inspector => new JsonPropertyNameInspector(inspector))
        .WithNamingConvention(HyphenatedNamingConvention.Instance).Build();

    public T Deserialize<T>(string input) => Deserializer.Deserialize<T>(input);
    public string Serialize(object graph) => Serializer.Serialize(graph);
    public IGitVersionConfiguration? ReadConfiguration(string input) => Deserialize<GitVersionConfiguration?>(input);

    private sealed class JsonPropertyNameInspector(ITypeInspector innerTypeDescriptor) : TypeInspectorSkeleton
    {
        public override string GetEnumName(Type enumType, string name) => innerTypeDescriptor.GetEnumName(enumType, name);

        public override string GetEnumValue(object enumValue) => innerTypeDescriptor.GetEnumValue(enumValue);

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) =>
            innerTypeDescriptor.GetProperties(type, container)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .Select(IPropertyDescriptor (p) =>
                {
                    var descriptor = new PropertyDescriptor(p);
                    var member = p.GetCustomAttribute<JsonPropertyNameAttribute>();
                    if (member is { Name: not null })
                    {
                        descriptor.Name = member.Name;
                    }

                    return descriptor;
                })
                .OrderBy(p => p.Order);
    }
}
