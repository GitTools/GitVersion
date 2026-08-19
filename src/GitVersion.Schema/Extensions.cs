using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using GitVersion.Configuration;
using Json.More;
using Json.Schema;

namespace GitVersion.Schema;

internal static class Extensions
{
    private const string AdditionalPropertiesJsonPropertyName = "additionalProperties";
    private const string PropertiesJsonPropertyName = "properties";

    extension(JsonSchema jsonSchema)
    {
        public void WriteToFile(string outputFileName, bool orderPropertiesByName = false)
        {
            using var jsonDocument = jsonSchema.ToJsonDocument();

            WriteToFile(jsonDocument, outputFileName, orderPropertiesByName);
        }

        public void WriteV7ConfigurationToFile(string outputFileName)
        {
            using var jsonDocument = jsonSchema.ToJsonDocument();
            var schema = JsonNode.Parse(jsonDocument.RootElement.GetRawText())?.AsObject()
                         ?? throw new InvalidOperationException("Could not materialize the configuration schema.");
            var properties = schema[PropertiesJsonPropertyName]?.AsObject()
                             ?? throw new InvalidOperationException("The configuration schema has no properties.");

            schema[PropertiesJsonPropertyName] = new JsonObject
            {
                [ConfigurationDocumentMapper.CalculationSectionName] = CreateSection(properties, output: false),
                [ConfigurationDocumentMapper.OutputSectionName] = CreateSection(properties, output: true)
            };
            schema[AdditionalPropertiesJsonPropertyName] = false;

            using var nestedSchema = JsonDocument.Parse(schema.ToJsonString());
            WriteToFile(nestedSchema, outputFileName, orderPropertiesByName: true);
        }
    }

    private static JsonObject CreateSection(JsonObject source, bool output)
    {
        JsonObject properties = [];
        foreach (var (propertyName, propertySchema) in source)
        {
            if (propertySchema is null)
            {
                continue;
            }

            if (propertyName == ConfigurationDocumentMapper.BranchesPropertyName)
            {
                properties[propertyName] = CreateBranches(propertySchema, output);
            }
            else if (ConfigurationDocumentMapper.IsOutputProperty(propertyName) == output)
            {
                properties[propertyName] = propertySchema.DeepClone();
            }
        }

        return new JsonObject
        {
            ["description"] = output
                ? "Settings that affect assembly, build-server, and formatted version output."
                : "Settings that participate in semantic-version calculation.",
            ["type"] = "object",
            [PropertiesJsonPropertyName] = properties,
            [AdditionalPropertiesJsonPropertyName] = false
        };
    }

    private static JsonNode CreateBranches(JsonNode source, bool output)
    {
        var branches = source.DeepClone().AsObject();
        var branchConfiguration = branches[AdditionalPropertiesJsonPropertyName]?.AsObject()
                                  ?? throw new InvalidOperationException("The branch configuration schema is missing.");
        var branchProperties = branchConfiguration[PropertiesJsonPropertyName]?.AsObject()
                               ?? throw new InvalidOperationException("The branch configuration schema has no properties.");
        JsonObject filteredBranchProperties = [];
        foreach (var (propertyName, propertySchema) in branchProperties)
        {
            if (propertySchema is not null
                && ConfigurationDocumentMapper.IsOutputBranchProperty(propertyName) == output)
            {
                filteredBranchProperties[propertyName] = propertySchema.DeepClone();
            }
        }

        branchConfiguration[PropertiesJsonPropertyName] = filteredBranchProperties;
        branchConfiguration[AdditionalPropertiesJsonPropertyName] = false;
        return branches;
    }

    private static void WriteToFile(JsonDocument jsonDocument, string outputFileName, bool orderPropertiesByName)
    {
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
                    WriteElement(writer, property.Value, property.NameEquals(PropertiesJsonPropertyName));
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
