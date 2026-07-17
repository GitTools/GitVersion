namespace GitVersion.Git;

/// <summary>Represents a Git branch, exposing its tip commit and tracking information.</summary>
public interface IBranch : IEquatable<IBranch?>, IComparable<IBranch>, INamedReference, ICommitish
{
    /// <summary>Gets the most recent commit on this branch, or <see langword="null"/> for an empty branch.</summary>
    ICommit? Tip { get; }

    /// <summary>Gets a value indicating whether this branch is a remote-tracking branch.</summary>
    bool IsRemote { get; }

    /// <summary>Gets a value indicating whether this branch tracks a remote branch.</summary>
    bool IsTracking { get; }

    /// <summary>Gets a value indicating whether HEAD is in a detached state pointing at this branch's tip.</summary>
    bool IsDetachedHead { get; }

    /// <summary>Gets the ordered sequence of commits reachable from this branch's tip.</summary>
    ICommitCollection Commits { get; }
}
