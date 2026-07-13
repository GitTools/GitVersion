namespace GitVersion.Testing;

/// <summary>
///     A lightweight view of a tag in a <see cref="TestRepository" />.
/// </summary>
public sealed class TestTag
{
    private readonly TestRepository repository;
    private readonly string targetSha;

    internal TestTag(TestRepository repository, string friendlyName, string targetSha)
    {
        this.repository = repository;
        this.targetSha = targetSha;
        FriendlyName = friendlyName;
    }

    public string FriendlyName { get; }

    public string CanonicalName => $"refs/tags/{FriendlyName}";

    /// <summary>
    ///     The commit the tag (peeled, for annotated tags) points at.
    /// </summary>
    public TestCommit Target => this.repository.LookupRequired(this.targetSha);

    public override string ToString() => CanonicalName;
}
