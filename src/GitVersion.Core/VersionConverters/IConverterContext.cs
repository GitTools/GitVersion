namespace GitVersion.VersionConverters
{
    public interface IConverterContext
    {
        public string WorkingDirectory { get; }
    }
}
