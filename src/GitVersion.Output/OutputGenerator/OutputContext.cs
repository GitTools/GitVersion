namespace GitVersion.Output.OutputGenerator;

internal readonly record struct OutputContext(string? OutputFile, bool? UpdateBuildNumber) : IConverterContext;
