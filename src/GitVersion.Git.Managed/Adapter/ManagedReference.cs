using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class ManagedReference : IReference
{
    private static readonly LambdaEqualityHelper<IReference> equalityHelper = new(x => x.Name.Canonical);
    private static readonly LambdaKeyComparer<IReference, string> comparerHelper = new(x => x.Name.Canonical);

    private readonly GitReference innerReference;

    internal ManagedReference(GitReference reference)
    {
        this.innerReference = reference.NotNull();
        Name = new ReferenceName(reference.CanonicalName);

        if (!reference.IsSymbolic && reference.ObjectId is { } objectId)
        {
            ReferenceTargetId = new ManagedObjectId(objectId);
        }
    }

    public ReferenceName Name { get; }
    public IObjectId? ReferenceTargetId { get; }

    public string TargetIdentifier =>
        this.innerReference.IsSymbolic
            ? this.innerReference.SymbolicTargetName!
            : this.innerReference.ObjectId!.Value.ToString();

    public int CompareTo(IReference? other) => comparerHelper.Compare(this, other);
    public bool Equals(IReference? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals(obj as IReference);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name.ToString();
}
