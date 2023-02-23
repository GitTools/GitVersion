using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;

namespace GitVersion;

internal class GitObject : IGitObject
{
    private static readonly LambdaEqualityHelper<IGitObject> EqualityHelper = new(x => x.Id);
    private static readonly LambdaKeyComparer<IGitObject, string> ComparerHelper = new(x => x.Sha);

    private readonly LibGit2Sharp.GitObject innerGitObject;

    internal GitObject(LibGit2Sharp.GitObject gitObject)
    {
        this.innerGitObject = gitObject.NotNull();
        Id = new ObjectId(gitObject.Id);
        Sha = gitObject.Sha;
    }

    public int CompareTo(IGitObject? other) => ComparerHelper.Compare(this, other);
    public bool Equals(IGitObject? other) => EqualityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals((obj as IGitObject));
    public override int GetHashCode() => EqualityHelper.GetHashCode(this);
    public override string ToString() => Id.ToString(7);

    public IObjectId Id { get; }
    public string Sha { get; }
    public static implicit operator LibGit2Sharp.GitObject(GitObject d) => d.NotNull().innerGitObject;
}
