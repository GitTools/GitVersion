using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;

namespace GitVersion;

internal sealed class Commit : GitObject, ICommit
{
    private static readonly LambdaEqualityHelper<ICommit> EqualityHelper = new(x => x.Id);
    private static readonly LambdaKeyComparer<ICommit, string> ComparerHelper = new(x => x.Sha);

    private readonly LibGit2Sharp.Commit innerCommit;

    internal Commit(LibGit2Sharp.Commit commit) : base(commit)
    {
        this.innerCommit = commit.NotNull();
        Parents = commit.Parents.Select(parent => new Commit(parent));
        When = commit.Committer.When;
    }

    public int CompareTo(ICommit? other) => ComparerHelper.Compare(this, other);
    public bool Equals(ICommit? other) => EqualityHelper.Equals(this, other);
    public IEnumerable<ICommit> Parents { get; }
    public DateTimeOffset When { get; }
    public string Message => this.innerCommit.Message;
    public override bool Equals(object? obj) => Equals((obj as ICommit));
    public override int GetHashCode() => EqualityHelper.GetHashCode(this);
    public override string ToString() => $"{Id.ToString(7)} {this.innerCommit.MessageShort}";
    public static implicit operator LibGit2Sharp.Commit(Commit d) => d.NotNull().innerCommit;
}
