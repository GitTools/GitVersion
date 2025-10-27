using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class RemoteCollection : IRemoteCollection
{
    private readonly LibGit2Sharp.RemoteCollection innerCollection;
    private readonly GitRepository repo;
    private Lazy<IReadOnlyCollection<IRemote>> remotes = null!;

    internal RemoteCollection(LibGit2Sharp.RemoteCollection collection, GitRepository repo)
    {
        this.innerCollection = collection.NotNull();
        this.repo = repo.NotNull();
        InitializeRemotesLazy();
    }

    public IEnumerator<IRemote> GetEnumerator() => this.remotes.Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IRemote? this[string name]
    {
        get
        {
            var remote = this.innerCollection[name];
            return remote is null ? null : this.repo.GetOrCreate(remote);
        }
    }

    public void Remove(string remoteName) =>
        RepositoryExtensions.RunSafe(() =>
        {
            this.innerCollection.Remove(remoteName);
            InitializeRemotesLazy();
        });

    public void Update(string remoteName, string refSpec) =>
        RepositoryExtensions.RunSafe(() =>
        {
            this.innerCollection.Update(remoteName, r => r.FetchRefSpecs.Add(refSpec));
            InitializeRemotesLazy();
        });

    private void InitializeRemotesLazy()
        => this.remotes = new Lazy<IReadOnlyCollection<IRemote>>(() => [.. this.innerCollection.Select(repo.GetOrCreate)]);
}
