namespace GitVersion.Output.OutputGenerator;

internal readonly struct OutputContext(string workingDirectory, string? outputFile, bool? updateBuildNumber)
    : IConverterContext
{
    public string WorkingDirectory { get; } = workingDirectory;
    public string? OutputFile { get; } = outputFile;
    public bool? UpdateBuildNumber { get; } = updateBuildNumber;
}
