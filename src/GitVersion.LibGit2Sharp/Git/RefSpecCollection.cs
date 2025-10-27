using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class RefSpecCollection : IRefSpecCollection
{
    private readonly Lazy<IReadOnlyCollection<IRefSpec>> refSpecs;

    internal RefSpecCollection(LibGit2Sharp.RefSpecCollection innerCollection, GitRepository repo)
    {
        innerCollection = innerCollection.NotNull();
        this.refSpecs = new Lazy<IReadOnlyCollection<IRefSpec>>(() => [.. innerCollection.Select(repo.GetOrCreate)]);
    }

    public IEnumerator<IRefSpec> GetEnumerator() => this.refSpecs.Value.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
