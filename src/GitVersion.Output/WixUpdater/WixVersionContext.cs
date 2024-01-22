namespace GitVersion.Output.WixUpdater;

internal readonly struct WixVersionContext(string workingDirectory) : IConverterContext
{
    public string WorkingDirectory { get; } = workingDirectory;
}
