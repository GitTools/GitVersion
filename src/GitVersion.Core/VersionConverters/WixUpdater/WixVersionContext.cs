namespace GitVersion.VersionConverters.WixUpdater
{
    public readonly struct WixVersionContext : IConverterContext
    {
        public WixVersionContext(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }

        public string WorkingDirectory { get; }
    }
}
