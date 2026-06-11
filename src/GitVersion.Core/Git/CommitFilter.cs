namespace GitVersion.Git;

/// <summary>Specifies criteria used to filter and order commits when querying a commit collection.</summary>
public record CommitFilter
{
    /// <summary>Gets a value indicating whether only the first-parent chain should be traversed.</summary>
    public bool FirstParentOnly { get; init; }

    /// <summary>Gets the commit, branch, or tag from which reachable commits are included.</summary>
    public object? IncludeReachableFrom { get; init; }

    /// <summary>Gets the commit, branch, or tag whose reachable commits are excluded from the result.</summary>
    public object? ExcludeReachableFrom { get; init; }

    /// <summary>Gets the ordering strategy applied to the resulting commits.</summary>
    public CommitSortStrategies SortBy { get; init; }
}

/// <summary>Specifies how commits are ordered when traversing the commit graph.</summary>
[Flags]
public enum CommitSortStrategies
{
    /// <summary>
    /// Sort the commits in no particular ordering;
    /// this sorting is arbitrary, implementation-specific
    /// and subject to change at any time.
    /// </summary>
    None = 0,

    /// <summary>
    /// Sort the commits in topological order
    /// (parents before children); this sorting mode
    /// can be combined with time sorting.
    /// </summary>
    Topological = (1 << 0),

    /// <summary>
    /// Sort the commits by commit time;
    /// this sorting mode can be combined with
    /// topological sorting.
    /// </summary>
    Time = (1 << 1),

    /// <summary>
    /// Iterate through the commits in reverse
    /// order; this sorting mode can be combined with
    /// any of the above.
    /// </summary>
    Reverse = (1 << 2)
}
