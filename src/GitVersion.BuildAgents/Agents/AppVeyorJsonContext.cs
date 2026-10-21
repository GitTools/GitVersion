namespace GitVersion.Agents;

internal sealed record AppVeyorBuildNumberRequest(string Version);

internal sealed record AppVeyorVariableRequest(string Name, string Value);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppVeyorBuildNumberRequest))]
[JsonSerializable(typeof(AppVeyorVariableRequest))]
internal partial class AppVeyorJsonContext : JsonSerializerContext;
