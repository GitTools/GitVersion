using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitVersionExe.Tests
{
    public class NumberToStringConverter : JsonConverter<string>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(string) == typeToConvert;
        }
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number || reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.Clone().ToString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
