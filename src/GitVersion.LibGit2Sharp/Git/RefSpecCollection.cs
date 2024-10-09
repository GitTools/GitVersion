using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class RefSpecCollection : IRefSpecCollection
{
    private readonly Lazy<IReadOnlyCollection<IRefSpec>> refSpecs;

    internal RefSpecCollection(LibGit2Sharp.RefSpecCollection collection)
    {
        collection = collection.NotNull();
        this.refSpecs = new Lazy<IReadOnlyCollection<IRefSpec>>(() => collection.Select(tag => new RefSpec(tag)).ToArray());
    }

    public IEnumerator<IRefSpec> GetEnumerator() => this.refSpecs.Value.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
