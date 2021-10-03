namespace GitVersion;

internal sealed class TagCollection : ITagCollection
{
    private readonly LibGit2Sharp.TagCollection innerCollection;
    internal TagCollection(LibGit2Sharp.TagCollection collection) => this.innerCollection = collection;

    public IEnumerator<ITag> GetEnumerator() => this.innerCollection.Select(tag => new Tag(tag)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
