using System.Text.Json.Serialization;

namespace GitVersion.NativeCapture.Validation;

internal sealed record CaptureReport(bool Passed, List<string> Checks, string? Error);

[JsonSerializable(typeof(CaptureReport))]
internal partial class CaptureJsonContext : JsonSerializerContext;
