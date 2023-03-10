namespace GitVersion.Configuration
{
    public interface IIgnoreConfiguration
    {
        DateTimeOffset? Before { get; }

        IReadOnlyList<string> Shas { get; }
    }
}
