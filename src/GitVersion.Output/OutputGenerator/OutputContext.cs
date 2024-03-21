namespace GitVersion.Output.OutputGenerator;

internal readonly record struct OutputContext(string WorkingDirectory, string? OutputFile, bool? UpdateBuildNumber) : IConverterContext;
