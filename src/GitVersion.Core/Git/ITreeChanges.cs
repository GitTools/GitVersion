namespace GitVersion.Git;

public interface ITreeChanges
{
    IEnumerable<string> Paths { get; }
}
