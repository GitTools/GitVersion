using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion.VersionCalculation;

public class VersionStrategiesConverter : IYamlTypeConverter
{
    public static readonly IYamlTypeConverter Instance = new VersionStrategiesConverter();

    public bool Accepts(Type type)
    {
        return type == typeof(VersionStrategies[]);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        string data = parser.Consume<Scalar>().Value;

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)  // see height_in_inches in sample yml 
            .Build();

        VersionStrategies[] strategies = deserializer.Deserialize<VersionStrategies[]>(data);

        return strategies;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        // Convert from an object to text during serialization.
        throw new NotImplementedException();
    }
}
