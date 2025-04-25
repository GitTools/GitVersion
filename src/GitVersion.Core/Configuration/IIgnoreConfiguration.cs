namespace GitVersion.Configuration;

public interface IIgnoreConfiguration
{
    DateTimeOffset? Before { get; }

    IReadOnlySet<string> Shas { get; }

    bool IsEmpty { get; }
}
