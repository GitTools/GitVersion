using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion;

internal sealed class Tag : ITag
{
    private static readonly LambdaEqualityHelper<ITag> equalityHelper = new(x => x.Name.Canonical);
    private static readonly LambdaKeyComparer<ITag, string> comparerHelper = new(x => x.Name.Canonical);

    private readonly LibGit2Sharp.Tag innerTag;
    internal Tag(LibGit2Sharp.Tag tag)
    {
        this.innerTag = tag;
        Name = new ReferenceName(this.innerTag.CanonicalName);
    }
    public ReferenceName Name { get; }

    public int CompareTo(ITag other) => comparerHelper.Compare(this, other);
    public bool Equals(ITag? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object obj) => Equals((obj as ITag)!);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name.ToString();
    public string TargetSha => this.innerTag.Target.Sha;

    public ICommit? PeeledTargetCommit()
    {
        var target = this.innerTag.Target;

        while (target is TagAnnotation annotation)
        {
            target = annotation.Target;
        }

        return target is LibGit2Sharp.Commit commit ? new Commit(commit) : null;
    }
}
