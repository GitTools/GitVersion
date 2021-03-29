using System;
using System.IO;
using GitVersion.Model.Configuration;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GitVersion.Configuration
{
    public class ConfigSerializer
    {
        public static Config Read(TextReader reader)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .WithTypeConverter(new YamlNullableEnumTypeConverter())
                .Build();
            var deserialize = deserializer.Deserialize<Config>(reader);
            return deserialize ?? new Config();
        }

        public static void Write(Config config, TextWriter writer)
        {
            var serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .WithTypeConverter(new YamlNullableEnumTypeConverter())
                .Build();
            serializer.Serialize(writer, config);
        }
    }

    public class YamlNullableEnumTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return Nullable.GetUnderlyingType(type)?.IsEnum ?? false;
        }

        public object ReadYaml(IParser parser, Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? throw new ArgumentException("Expected nullable enum type for ReadYaml");

            if (parser.Accept<NodeEvent>(out var @event))
            {
                if (NodeIsNull(@event))
                {
                    parser.SkipThisAndNestedEvents();
                    return null;
                }
            }

            var scalar = parser.Consume<Scalar>();
            try
            {
                return Enum.Parse(type, scalar.Value, ignoreCase: true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Invalid value: \"{scalar.Value}\" for {type.Name}", ex);
            }
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? throw new ArgumentException("Expected nullable enum type for WriteYaml");

            if (value != null)
            {
                var toWrite = Enum.GetName(type, value) ?? throw new InvalidOperationException($"Invalid value {value} for enum: {type}");
                emitter.Emit(new Scalar(null, null, toWrite, ScalarStyle.Any, true, false));
            }
        }

        private static bool NodeIsNull(NodeEvent nodeEvent)
        {
            // http://yaml.org/type/null.html

            if (nodeEvent.Tag == "tag:yaml.org,2002:null")
            {
                return true;
            }

            if (nodeEvent is Scalar scalar && scalar.Style == ScalarStyle.Plain)
            {
                var value = scalar.Value;
                return value is "" or "~" or "null" or "Null" or "NULL";
            }

            return false;
        }
    }
}
