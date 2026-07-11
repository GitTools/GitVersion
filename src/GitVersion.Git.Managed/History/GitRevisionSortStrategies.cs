namespace GitVersion.Git;

/// <summary>
/// The sort orders supported by <see cref="GitRevisionWalker"/>, mirroring the strategies
/// GitVersion uses when querying commit history.
/// </summary>
[Flags]
internal enum GitRevisionSortStrategies
{
    /// <summary>
    /// Git's default ordering: reverse chronological by committer date.
    /// </summary>
    None = 0,

    /// <summary>
    /// Topological ordering: a commit is only emitted after all of the emitted
    /// commits which list it as a parent.
    /// </summary>
    Topological = 1,

    /// <summary>
    /// Reverse chronological ordering by committer date.
    /// </summary>
    Time = 2,

    /// <summary>
    /// Reverses the selected ordering.
    /// </summary>
    Reverse = 4
}
