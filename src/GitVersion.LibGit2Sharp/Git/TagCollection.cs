using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class TagCollection : ITagCollection
{
    private readonly Lazy<IReadOnlyCollection<ITag>> tags;

    internal TagCollection(LibGit2Sharp.TagCollection collection, LibGit2Sharp.Diff diff, GitRepository repo)
    {
        collection.NotNull();
        diff.NotNull();
        repo.NotNull();
        this.tags = new Lazy<IReadOnlyCollection<ITag>>(() => [.. collection.Select(tag => repo.GetOrCreate(tag, diff))]);
    }

    public IEnumerator<ITag> GetEnumerator()
        => this.tags.Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
