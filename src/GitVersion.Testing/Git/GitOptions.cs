namespace GitVersion.Testing;

/// <summary>
///     Options controlling how <see cref="TestRepository.Commit(string, Signature, Signature, CommitOptions?)" /> creates a commit.
/// </summary>
public sealed class CommitOptions
{
    public bool AmendPreviousCommit { get; set; }
    public bool AllowEmptyCommit { get; set; }
}

/// <summary>
///     Options controlling how merges are performed.
/// </summary>
public sealed class MergeOptions
{
    public FastForwardStrategy FastForwardStrategy { get; set; }
}

public enum FastForwardStrategy
{
    Default = 0,
    NoFastForward = 1,
    FastForwardOnly = 2
}

/// <summary>
///     Options controlling fetch behavior. Present for call-site compatibility; fetches always use default behavior.
/// </summary>
public sealed class FetchOptions;

/// <summary>
///     Options controlling pull behavior. Present for call-site compatibility; pulls always use default behavior.
/// </summary>
public sealed class PullOptions;
