using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class Remote : IRemote
{
    private static readonly LambdaEqualityHelper<IRemote> equalityHelper = new(x => x.Name);
    private static readonly LambdaKeyComparer<IRemote, string> comparerHelper = new(x => x.Name);

    private readonly LibGit2Sharp.Remote innerRemote;
    private readonly GitRepositoryCache repositoryCache;

    internal Remote(LibGit2Sharp.Remote remote, GitRepositoryCache repositoryCache)
    {
        this.innerRemote = remote.NotNull();
        this.repositoryCache = repositoryCache.NotNull();
    }

    public int CompareTo(IRemote? other) => comparerHelper.Compare(this, other);
    public bool Equals(IRemote? other) => equalityHelper.Equals(this, other);
    public string Name => this.innerRemote.Name;
    public string Url => this.innerRemote.Url;

    private IEnumerable<IRefSpec> RefSpecs
    {
        get
        {
            var refSpecs = this.innerRemote.RefSpecs;
            return refSpecs is null
                ? []
                : new RefSpecCollection((LibGit2Sharp.RefSpecCollection)refSpecs, this.repositoryCache);
        }
    }

    public IEnumerable<IRefSpec> FetchRefSpecs => RefSpecs.Where(x => x.Direction == RefSpecDirection.Fetch);
    public IEnumerable<IRefSpec> PushRefSpecs => RefSpecs.Where(x => x.Direction == RefSpecDirection.Push);
    public override bool Equals(object? obj) => Equals(obj as IRemote);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name;
}
