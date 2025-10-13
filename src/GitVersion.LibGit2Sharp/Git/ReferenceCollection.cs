using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class ReferenceCollection : IReferenceCollection
{
    private readonly LibGit2Sharp.ReferenceCollection innerCollection;
    private readonly GitRepository repo;
    private readonly Lazy<IReadOnlyCollection<IReference>> references;

    internal ReferenceCollection(LibGit2Sharp.ReferenceCollection collection, GitRepository repo)
    {
        this.innerCollection = collection.NotNull();
        this.repo = repo.NotNull();
        this.references = new Lazy<IReadOnlyCollection<IReference>>(() => [.. this.innerCollection.Select(repo.GetOrWrap)]);
    }

    public IEnumerator<IReference> GetEnumerator() => this.references.Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(string name, string canonicalRefNameOrObject, bool allowOverwrite = false)
        => this.innerCollection.Add(name, canonicalRefNameOrObject, allowOverwrite);

    public void UpdateTarget(IReference directRef, IObjectId targetId)
        => RepositoryExtensions.RunSafe(() => this.innerCollection.UpdateTarget((Reference)directRef, (ObjectId)targetId));

    public IReference? this[string name]
    {
        get
        {
            var reference = this.innerCollection[name];
            return reference is null ? null : this.repo.GetOrWrap(reference);
        }
    }

    public IReference? this[ReferenceName referenceName] => this[referenceName.Canonical];

    public IReference? Head => this["HEAD"];

    public IEnumerable<IReference> FromGlob(string prefix) => this.innerCollection.FromGlob(prefix).Select(reference => (IReference)this.repo.GetOrWrap(reference));
}
