using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class RefSpecCollection : IRefSpecCollection
{
    private readonly Lazy<IReadOnlyCollection<IRefSpec>> refSpecs;

    internal RefSpecCollection(LibGit2Sharp.RefSpecCollection innerCollection, GitRepositoryCache repositoryCache)
    {
        innerCollection = innerCollection.NotNull();
        this.refSpecs = new Lazy<IReadOnlyCollection<IRefSpec>>(() => [.. innerCollection.Select(repositoryCache.GetOrWrap)]);
    }

    public IEnumerator<IRefSpec> GetEnumerator() => this.refSpecs.Value.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
