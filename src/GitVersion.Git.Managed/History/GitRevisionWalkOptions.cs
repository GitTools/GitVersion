namespace GitVersion.Git;

/// <summary>
/// Describes a revision walk: which commits to start from, which histories to exclude,
/// the sort order, and whether to follow only first parents.
/// </summary>
internal sealed class GitRevisionWalkOptions
{
    /// <summary>
    /// Gets the commits whose histories are included in the walk.
    /// </summary>
    public IList<GitObjectId> Include { get; } = [];

    /// <summary>
    /// Gets the commits whose histories (including the commits themselves) are excluded from the walk.
    /// </summary>
    public IList<GitObjectId> Exclude { get; } = [];

    /// <summary>
    /// Gets the sort order of the walk.
    /// </summary>
    public GitRevisionSortStrategies Sort { get; init; } = GitRevisionSortStrategies.None;

    /// <summary>
    /// Gets a value indicating whether only the first parent of each commit is followed.
    /// </summary>
    public bool FirstParentOnly { get; init; }
}
