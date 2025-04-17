namespace GitVersion.Output.GitVersionInfo;

internal readonly record struct GitVersionInfoContext(
    string WorkingDirectory,
    string FileName,
    string? TargetNamespace = null)
    : IConverterContext;
