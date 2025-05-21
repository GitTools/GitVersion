using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class TagCollection : ITagCollection
{
    private readonly Lazy<IReadOnlyCollection<ITag>> tags;

    internal TagCollection(LibGit2Sharp.TagCollection collection, LibGit2Sharp.Diff diff)
    {
        collection = collection.NotNull();
        this.tags = new Lazy<IReadOnlyCollection<ITag>>(() => [.. collection.Select(tag => new Tag(tag, diff))]);
    }

    public IEnumerator<ITag> GetEnumerator()
        => this.tags.Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
