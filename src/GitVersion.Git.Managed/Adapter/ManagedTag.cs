using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class ManagedTag : ITag
{
    private static readonly LambdaEqualityHelper<ITag> equalityHelper = new(x => x.Name.Canonical);
    private static readonly LambdaKeyComparer<ITag, string> comparerHelper = new(x => x.Name.Canonical);

    private readonly GitReference reference;
    private readonly ManagedGitRepository repository;
    private readonly Lazy<ICommit> commitLazy;
    private readonly Lazy<string> targetShaLazy;

    internal ManagedTag(GitReference reference, ManagedGitRepository repository)
    {
        this.reference = reference.NotNull();
        this.repository = repository.NotNull();
        this.commitLazy = new(() => this.repository.Session.PeelToCommit(this.reference));
        this.targetShaLazy = new(() => this.repository.Session.GetTagTargetSha(this.reference));
        Name = new(reference.CanonicalName);
    }

    public ReferenceName Name { get; }

    /// <summary>
    /// Matches libgit2: for an annotated tag this is the sha of the object the tag
    /// annotation points at (one dereference, usually the commit); for a lightweight
    /// tag it is the ref target itself.
    /// </summary>
    public string TargetSha => this.targetShaLazy.Value;

    public ICommit Commit => this.commitLazy.Value;

    public int CompareTo(ITag? other) => comparerHelper.Compare(this, other);
    public bool Equals(ITag? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals(obj as ITag);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name.ToString();
}
