using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class ManagedObjectId : IObjectId
{
    private static readonly LambdaEqualityHelper<IObjectId> equalityHelper = new(x => x.Sha);
    private static readonly LambdaKeyComparer<IObjectId, string> comparerHelper = new(x => x.Sha);

    internal ManagedObjectId(GitObjectId objectId)
    {
        ObjectId = objectId;
        Sha = objectId.ToString();
    }

    internal GitObjectId ObjectId { get; }

    public string Sha { get; }

    public int CompareTo(IObjectId? other) => comparerHelper.Compare(this, other);
    public bool Equals(IObjectId? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals(obj as IObjectId);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => ToString(7);
    public string ToString(int prefixLength) => ObjectId.ToString(prefixLength);
}
