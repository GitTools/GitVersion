namespace GitVersion.Output.WixUpdater;

internal readonly struct WixVersionContext : IConverterContext
{
    public WixVersionContext(string workingDirectory) =>
        WorkingDirectory = workingDirectory;

    public string WorkingDirectory { get; }
}
