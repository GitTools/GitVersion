using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class TagCollection : ITagCollection
{
    private readonly Lazy<IReadOnlyCollection<ITag>> tags;

    internal TagCollection(LibGit2Sharp.TagCollection collection)
    {
        collection = collection.NotNull();
        this.tags = new Lazy<IReadOnlyCollection<ITag>>(() => [.. collection.Select(tag => new Tag(tag))]);
    }

    public IEnumerator<ITag> GetEnumerator()
        => this.tags.Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
