namespace GitVersion.Testing;

/// <summary>
///     A lightweight view of a branch in a <see cref="TestRepository" />.
/// </summary>
public sealed class TestBranch
{
    private readonly TestRepository repository;
    private readonly string target;

    internal TestBranch(TestRepository repository, string canonicalName, string friendlyName, string? target = null)
    {
        this.repository = repository;
        this.target = target ?? canonicalName;
        CanonicalName = canonicalName;
        FriendlyName = friendlyName;
    }

    public string CanonicalName { get; }

    public string FriendlyName { get; }

    public bool IsRemote => CanonicalName.StartsWith("refs/remotes/", StringComparison.Ordinal);

    /// <summary>
    ///     The commit the branch points at.
    /// </summary>
    public TestCommit Tip => this.repository.LookupRequired(this.target);

    /// <summary>
    ///     The commits reachable from the branch tip, newest first.
    /// </summary>
    public IReadOnlyList<TestCommit> Commits => this.repository.GetLog(this.target);

    public override string ToString() => CanonicalName;
}
