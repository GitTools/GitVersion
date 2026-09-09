namespace GitVersion.OutputVariables;

// Legacy string variables serialize null as "". Source variables explicitly represent an external source as JSON null.
internal sealed class SourceVariableJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
