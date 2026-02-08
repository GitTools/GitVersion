using System.Text.Encodings.Web;

namespace GitVersion.OutputVariables;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    Converters = [typeof(VersionVariablesJsonStringConverter)])]
[JsonSerializable(typeof(VersionVariablesJsonModel))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class VersionVariablesJsonContext : JsonSerializerContext
{
    public static VersionVariablesJsonContext Custom => field ??= new VersionVariablesJsonContext(
        new JsonSerializerOptions(Default.Options)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
}
