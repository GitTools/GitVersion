namespace GitVersion.Configuration;

public interface IIgnoreConfiguration
{
    DateTimeOffset? Before { get; }

    IReadOnlySet<string> Shas { get; }

    bool IsEmpty => Before == null && Shas.Count == 0;
}
