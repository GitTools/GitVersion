namespace GitVersion.Testing;

/// <summary>
///     A lightweight, immutable view of a commit in a <see cref="TestRepository" />.
///     Metadata other than the sha is hydrated lazily (a single <c>git log</c>) so that the very common
///     case of creating a commit and only reading its <see cref="Sha" /> costs no extra git invocation.
/// </summary>
public sealed class TestCommit
{
    private readonly TestRepository repository;
    private string? message;
    private Signature? author;
    private Signature? committer;
    private string[]? parentShas;

    /// <summary>
    ///     Creates a fully-populated commit view (used when the metadata has already been read).
    /// </summary>
    internal TestCommit(TestRepository repository, string sha, string message, Signature author, Signature committer, string[] parentShas)
    {
        this.repository = repository;
        this.message = message;
        this.author = author;
        this.committer = committer;
        this.parentShas = parentShas;
        Sha = sha;
    }

    /// <summary>
    ///     Creates a commit view whose metadata is hydrated on first access.
    /// </summary>
    internal TestCommit(TestRepository repository, string sha)
    {
        this.repository = repository;
        Sha = sha;
    }

    public string Sha { get; }

    public string Message => this.message ??= Hydrate().message!;

    public string MessageShort => Message.Split('\n')[0];

    public Signature Author => this.author ??= Hydrate().author!;

    public Signature Committer => this.committer ??= Hydrate().committer!;

    public IReadOnlyList<TestCommit> Parents => [.. (this.parentShas ??= Hydrate().parentShas!).Select(this.repository.LookupRequired)];

    public override string ToString() => Sha.Length >= 7 ? Sha[..7] : Sha;

    public override bool Equals(object? obj) => obj is TestCommit other && other.Sha == Sha;

    public override int GetHashCode() => Sha.GetHashCode();

    private TestCommit Hydrate()
    {
        var hydrated = this.repository.LookupRequired(Sha);
        this.message = hydrated.message;
        this.author = hydrated.author;
        this.committer = hydrated.committer;
        this.parentShas = hydrated.parentShas;
        return hydrated;
    }
}
