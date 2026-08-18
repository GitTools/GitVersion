using System.Text.Encodings.Web;
using Json.More;
using Json.Schema;

namespace GitVersion.Schema;

internal static class Extensions
{
    extension(JsonSchema jsonSchema)
    {
        public void WriteToFile(string outputFileName, bool orderPropertiesByName = false)
        {
            var jsonDocument = jsonSchema.ToJsonDocument();

            using var fs = File.Create(outputFileName);
            using var writer = new Utf8JsonWriter(fs, new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            if (orderPropertiesByName)
            {
                WriteElement(writer, jsonDocument.RootElement);
            }
            else
            {
                jsonDocument.WriteTo(writer);
            }
            writer.Flush();
            fs.Flush();
        }
    }

    private static void WriteElement(Utf8JsonWriter writer, JsonElement element, bool orderMembers = false)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                IEnumerable<JsonProperty> properties = element.EnumerateObject();
                if (orderMembers)
                {
                    properties = properties.OrderBy(property => property.Name, StringComparer.Ordinal);
                }

                foreach (var property in properties)
                {
                    writer.WritePropertyName(property.Name);
                    WriteElement(writer, property.Value, property.NameEquals("properties"));
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(writer, item);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }
}
