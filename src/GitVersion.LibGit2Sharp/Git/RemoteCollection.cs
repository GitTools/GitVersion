namespace GitVersion;

internal sealed class RemoteCollection : IRemoteCollection
{
    private readonly LibGit2Sharp.RemoteCollection innerCollection;
    internal RemoteCollection(LibGit2Sharp.RemoteCollection collection) => this.innerCollection = collection;

    public IEnumerator<IRemote> GetEnumerator() => this.innerCollection.Select(reference => new Remote(reference)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IRemote? this[string name]
    {
        get
        {
            var remote = this.innerCollection[name];
            return remote is null ? null : new Remote(remote);
        }
    }

    public void Remove(string remoteName) => this.innerCollection.Remove(remoteName);
    public void Update(string remoteName, string refSpec) => this.innerCollection.Update(remoteName, r => r.FetchRefSpecs.Add(refSpec));
}
