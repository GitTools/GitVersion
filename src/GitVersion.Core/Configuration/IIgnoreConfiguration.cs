namespace GitVersion.Configuration;

public interface IIgnoreConfiguration
{
    DateTimeOffset? Before { get; }

    IReadOnlySet<string> Shas { get; }

    IReadOnlyCollection<string> Paths { get; }

    bool IsEmpty => Before == null && Shas.Count == 0 && Paths.Count == 0;
}
