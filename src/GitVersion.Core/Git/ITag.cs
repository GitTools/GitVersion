namespace GitVersion.Git;

/// <summary>Represents a Git tag (lightweight or annotated).</summary>
public interface ITag : IEquatable<ITag?>, IComparable<ITag>, INamedReference
{
    /// <summary>Gets the SHA of the object that this tag directly points to (the tag object SHA for annotated tags, or the commit SHA for lightweight tags).</summary>
    string TargetSha { get; }

    /// <summary>Gets the commit that this tag ultimately resolves to.</summary>
    ICommit Commit { get; }
}
