using GitVersion.Extensions;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion;

internal sealed class Tag : ITag
{
    private static readonly LambdaEqualityHelper<ITag> equalityHelper = new(x => x.Name.Canonical);
    private static readonly LambdaKeyComparer<ITag, string> comparerHelper = new(x => x.Name.Canonical);
    private readonly LibGit2Sharp.Tag innerTag;
    private readonly Lazy<ICommit?> _commitLazy;

    internal Tag(LibGit2Sharp.Tag tag)
    {
        this.innerTag = tag.NotNull();
        Name = new ReferenceName(this.innerTag.CanonicalName);
        _commitLazy = new Lazy<ICommit?>(PeeledTargetCommit);
    }

    public ReferenceName Name { get; }
    public int CompareTo(ITag? other) => comparerHelper.Compare(this, other);
    public bool Equals(ITag? other) => equalityHelper.Equals(this, other);
    public string? TargetSha => this.innerTag.Target.Sha;
    public ICommit Commit => _commitLazy.Value.NotNull();

    private ICommit? PeeledTargetCommit()
    {
        var target = this.innerTag.Target;

        while (target is TagAnnotation annotation)
        {
            target = annotation.Target;
        }

        return target is LibGit2Sharp.Commit commit ? new Commit(commit) : null;
    }

    public override bool Equals(object? obj) => Equals(obj as ITag);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name.ToString();
}
