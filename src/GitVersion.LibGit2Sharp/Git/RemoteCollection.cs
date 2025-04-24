using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class RemoteCollection : IRemoteCollection
{
    private readonly LibGit2Sharp.RemoteCollection innerCollection;
    private IReadOnlyCollection<IRemote>? remotes;

    internal RemoteCollection(LibGit2Sharp.RemoteCollection collection) => this.innerCollection = collection.NotNull();

    public IEnumerator<IRemote> GetEnumerator()
    {
        this.remotes ??= [.. this.innerCollection.Select(reference => new Remote(reference))];
        return this.remotes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IRemote? this[string name]
    {
        get
        {
            var remote = this.innerCollection[name];
            return remote is null ? null : new Remote(remote);
        }
    }

    public void Remove(string remoteName)
    {
        this.innerCollection.Remove(remoteName);
        this.remotes = null;
    }

    public void Update(string remoteName, string refSpec)
    {
        this.innerCollection.Update(remoteName, r => r.FetchRefSpecs.Add(refSpec));
        this.remotes = null;
    }
}
