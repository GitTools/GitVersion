using GitVersion.VersionCalculation;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion.Configuration;

internal class VersionStrategiesConverter : IYamlTypeConverter
{
    public static readonly IYamlTypeConverter Instance = new VersionStrategiesConverter();

    public bool Accepts(Type type) => type == typeof(VersionStrategies[]);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        List<VersionStrategies> strategies = [];

        if (parser.TryConsume<SequenceStart>(out var _))
        {
            while (!parser.TryConsume<SequenceEnd>(out var _))
            {
                var data = parser.Consume<Scalar>().Value;

                var strategy = Enum.Parse<VersionStrategies>(data);
                strategies.Add(strategy);
            }
        }
        else
        {
            var data = parser.Consume<Scalar>().Value;

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            strategies = deserializer.Deserialize<List<VersionStrategies>>(data);
        }

        return strategies.ToArray();
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var strategies = (VersionStrategies[])value!;

        var s = new SerializerBuilder()
            .JsonCompatible()
            .Build();
        var data = s.Serialize(strategies);

        emitter.Emit(new Scalar(data));
    }
}
