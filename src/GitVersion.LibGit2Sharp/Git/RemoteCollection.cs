using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class RemoteCollection : IRemoteCollection
{
    private readonly LibGit2Sharp.RemoteCollection innerCollection;
    private readonly GitRepositoryCache repositoryCache;
    private readonly Lazy<IReadOnlyCollection<IRemote>> remotes;

    internal RemoteCollection(LibGit2Sharp.RemoteCollection collection, GitRepositoryCache repositoryCache)
    {
        this.innerCollection = collection.NotNull();
        this.repositoryCache = repositoryCache.NotNull();
        this.remotes = new Lazy<IReadOnlyCollection<IRemote>>(() => [.. this.innerCollection.Select(repositoryCache.GetOrWrap)]);
    }

    public IEnumerator<IRemote> GetEnumerator() => this.remotes.Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IRemote? this[string name]
    {
        get
        {
            var remote = this.innerCollection[name];
            return remote is null ? null : this.repositoryCache.GetOrWrap(remote);
        }
    }

    public void Remove(string remoteName)
        => this.innerCollection.Remove(remoteName);

    public void Update(string remoteName, string refSpec)
        => this.innerCollection.Update(remoteName, r => r.FetchRefSpecs.Add(refSpec));
}
