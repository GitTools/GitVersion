using GitVersion.Extensions;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion.Git;

internal sealed class Tag : ITag
{
    private static readonly LambdaEqualityHelper<ITag> equalityHelper = new(x => x.Name.Canonical);
    private static readonly LambdaKeyComparer<ITag, string> comparerHelper = new(x => x.Name.Canonical);
    private readonly LibGit2Sharp.Tag innerTag;
    private readonly Diff diff;
    private readonly Lazy<ICommit?> commitLazy;
    private readonly GitRepositoryCache repositoryCache;

    internal Tag(LibGit2Sharp.Tag tag, Diff diff, GitRepositoryCache repositoryCache)
    {
        this.innerTag = tag.NotNull();
        this.diff = diff.NotNull();
        this.repositoryCache = repositoryCache.NotNull();
        this.commitLazy = new(PeeledTargetCommit);
        Name = new(this.innerTag.CanonicalName);
    }

    public ReferenceName Name { get; }
    public int CompareTo(ITag? other) => comparerHelper.Compare(this, other);
    public bool Equals(ITag? other) => equalityHelper.Equals(this, other);
    public string TargetSha => this.innerTag.Target.Sha;
    public ICommit Commit => this.commitLazy.Value.NotNull();

    private ICommit? PeeledTargetCommit()
    {
        var target = this.innerTag.Target;

        while (target is TagAnnotation annotation)
        {
            target = annotation.Target;
        }

        return target is LibGit2Sharp.Commit commit ? this.repositoryCache.GetOrWrap(commit, this.diff) : null;
    }

    public override bool Equals(object? obj) => Equals(obj as ITag);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name.ToString();
}
