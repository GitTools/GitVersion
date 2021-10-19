using GitVersion.Helpers;

namespace GitVersion;

internal class GitObject : IGitObject
{
    private static readonly LambdaEqualityHelper<IGitObject> equalityHelper = new(x => x.Id);
    private static readonly LambdaKeyComparer<IGitObject, string> comparerHelper = new(x => x.Sha);

    internal GitObject(LibGit2Sharp.GitObject innerGitObject)
    {
        Id = new ObjectId(innerGitObject.Id);
        Sha = innerGitObject.Sha;
    }

    public int CompareTo(IGitObject other) => comparerHelper.Compare(this, other);
    public bool Equals(IGitObject? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object obj) => Equals((obj as IGitObject)!);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Id.ToString(7);

    public IObjectId Id { get; }
    public string Sha { get; }
}
