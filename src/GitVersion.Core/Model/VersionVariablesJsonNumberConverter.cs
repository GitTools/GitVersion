using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GitVersion.Extensions;
using JetBrains.Annotations;

namespace GitVersion.OutputVariables;

public class VersionVariablesJsonNumberConverter : JsonConverter<string>
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(string);

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number && typeToConvert == typeof(string))
            return reader.GetString() ?? "";

        var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
        if (Utf8Parser.TryParse(span, out long number, out var bytesConsumed) && span.Length == bytesConsumed)
            return number.ToString();

        var data = reader.GetString();

        throw new InvalidOperationException($"'{data}' is not a correct expected value!")
        {
            Source = nameof(VersionVariablesJsonNumberConverter)
        };
    }

    public override void Write(Utf8JsonWriter writer, [CanBeNull] string value, JsonSerializerOptions options)
    {
        if (value.IsNullOrWhiteSpace())
        {
            writer.WriteNullValue();
        }
        else if (int.TryParse(value, out var number))
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            throw new InvalidOperationException($"'{value}' is not a correct expected value!")
            {
                Source = nameof(VersionVariablesJsonStringConverter)
            };
        }

    }

    public override bool HandleNull => true;

    private static bool NotAPaddedNumber(string value) => value != null && (value == "0" || !value.StartsWith("0"));
}
