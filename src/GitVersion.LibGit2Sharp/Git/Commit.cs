using GitVersion.Helpers;

namespace GitVersion;

internal sealed class Commit : GitObject, ICommit
{
    private static readonly LambdaEqualityHelper<ICommit> equalityHelper = new(x => x.Id);
    private static readonly LambdaKeyComparer<ICommit, string> comparerHelper = new(x => x.Sha);

    private readonly LibGit2Sharp.Commit innerCommit;

    internal Commit(LibGit2Sharp.Commit innerCommit) : base(innerCommit)
    {
        this.innerCommit = innerCommit;
        Parents = innerCommit.Parents.Select(parent => new Commit(parent));
        When = innerCommit.Committer.When;
    }

    public int CompareTo(ICommit other) => comparerHelper.Compare(this, other);
    public bool Equals(ICommit? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object obj) => Equals((obj as ICommit)!);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => $"{Id.ToString(7)} {this.innerCommit.MessageShort}";
    public static implicit operator LibGit2Sharp.Commit(Commit d) => d.innerCommit;

    public IEnumerable<ICommit> Parents { get; }
    public DateTimeOffset When { get; }

    public string Message => this.innerCommit.Message;
}
