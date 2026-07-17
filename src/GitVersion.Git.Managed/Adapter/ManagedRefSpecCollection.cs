namespace GitVersion.Git;

internal sealed class ManagedRefSpecCollection(IReadOnlyList<IRefSpec> refSpecs) : IRefSpecCollection
{
    private readonly IReadOnlyList<IRefSpec> refSpecs = refSpecs ?? throw new ArgumentNullException(nameof(refSpecs));

    public IEnumerator<IRefSpec> GetEnumerator() => this.refSpecs.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
