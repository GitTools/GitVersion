using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class ManagedTagCollection(ManagedGitRepository repository) : ITagCollection
{
    private readonly ManagedGitRepository repository = repository.NotNull();

    public IEnumerator<ITag> GetEnumerator() => this.repository.Session.Tags.Cast<ITag>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
