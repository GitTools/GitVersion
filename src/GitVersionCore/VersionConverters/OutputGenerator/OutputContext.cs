namespace GitVersion.VersionConverters.OutputGenerator
{
    public readonly struct OutputContext : IConverterContext
    {
        public OutputContext(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
        }

        public string WorkingDirectory { get; }
    }
}
