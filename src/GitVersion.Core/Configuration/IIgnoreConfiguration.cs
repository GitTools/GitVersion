namespace GitVersion.Configuration;

public interface IIgnoreConfiguration
{
    DateTimeOffset? Before { get; }

    IReadOnlySet<string> Shas { get; }

    IReadOnlySet<string> Paths { get; }

    bool IsEmpty { get; }
}
