namespace GitVersion.VersionConverters.OutputGenerator
{
    public readonly struct OutputContext : IConverterContext
    {
        public OutputContext(string workingDirectory, string outputFile)
        {
            WorkingDirectory = workingDirectory;
            OutputFile = outputFile;
        }

        public string WorkingDirectory { get; }
        public string OutputFile { get; }
    }
}
