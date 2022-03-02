namespace GitVersion;

internal sealed class RefSpecCollection : IRefSpecCollection
{
    private readonly LibGit2Sharp.RefSpecCollection innerCollection;
    internal RefSpecCollection(LibGit2Sharp.RefSpecCollection collection) => this.innerCollection = collection;
    public IEnumerator<IRefSpec> GetEnumerator() => this.innerCollection.Select(tag => new RefSpec(tag)).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
