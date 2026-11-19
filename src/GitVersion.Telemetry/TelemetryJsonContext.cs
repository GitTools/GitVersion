using System.Text.Json.Serialization;

namespace GitVersion.Telemetry;

internal sealed record TelemetryPayload(
    [property: JsonPropertyName("api_key")] string ProjectKey,
    [property: JsonPropertyName("event")] string Event,
    [property: JsonPropertyName("distinct_id")] string DistinctId,
    [property: JsonPropertyName("properties")] Dictionary<string, object> Properties);

[JsonSerializable(typeof(TelemetryPayload))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(string[]))]
internal partial class TelemetryJsonContext : JsonSerializerContext;
