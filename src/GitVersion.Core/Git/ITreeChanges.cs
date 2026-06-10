namespace GitVersion.Git;

/// <summary>Represents the set of file paths changed between two tree objects.</summary>
public interface ITreeChanges
{
    /// <summary>Gets the paths of all files that were added, modified, or deleted.</summary>
    IReadOnlyList<string> Paths { get; }
}
