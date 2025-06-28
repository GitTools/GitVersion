namespace GitVersion.Git;

public interface ITreeChanges
{
    IReadOnlyList<string> Paths { get; }
}
