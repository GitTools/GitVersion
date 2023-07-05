namespace GitVersion.Output.OutputGenerator;

internal readonly struct OutputContext : IConverterContext
{
    public OutputContext(string workingDirectory, string? outputFile, bool? updateBuildNumber)
    {
        WorkingDirectory = workingDirectory;
        OutputFile = outputFile;
        UpdateBuildNumber = updateBuildNumber;
    }

    public string WorkingDirectory { get; }
    public string? OutputFile { get; }
    public bool? UpdateBuildNumber { get; }
}
