using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class ManagedCommit : ICommit
{
    private static readonly LambdaEqualityHelper<ICommit> equalityHelper = new(x => x.Id);
    private static readonly LambdaKeyComparer<ICommit, string> comparerHelper = new(x => x.Sha);

    private readonly GitCommit innerCommit;
    private readonly ManagedGitRepository repository;
    private readonly Lazy<IReadOnlyList<ICommit>> parentsLazy;

    internal ManagedCommit(GitCommit innerCommit, ManagedGitRepository repository)
    {
        this.innerCommit = innerCommit;
        this.repository = repository.NotNull();
        this.parentsLazy = new(() =>
            [.. innerCommit.Parents
                .Select(parentId => this.repository.Session.TryGetCommit(parentId))
                .Where(parent => parent is not null)
                .Cast<ICommit>()]);
        Id = new ManagedObjectId(innerCommit.Sha);
        Sha = Id.Sha;
        When = innerCommit.Committer.When;
    }

    internal GitObjectId ObjectId => this.innerCommit.Sha;
    internal GitObjectId TreeId => this.innerCommit.Tree;
    internal GitObjectId? FirstParentId => this.innerCommit.Parents.Count > 0 ? this.innerCommit.Parents[0] : null;

    public IReadOnlyList<ICommit> Parents => this.parentsLazy.Value;
    public IObjectId Id { get; }
    public string Sha { get; }
    public DateTimeOffset When { get; }
    public string Message => this.innerCommit.Message;
    public bool IsMergeCommit => Parents.Count >= 2;
    public IReadOnlyList<string> DiffPaths => this.repository.Session.GetDiffPaths(this);

    public int CompareTo(ICommit? other) => comparerHelper.Compare(this, other);
    public bool Equals(ICommit? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals(obj as ICommit);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => $"'{Id.ToString(7)}' - {MessageShort}";

    private string MessageShort
    {
        get
        {
            var message = this.innerCommit.Message;
            var lineEnd = message.IndexOf('\n');
            return (lineEnd < 0 ? message : message[..lineEnd]).TrimEnd('\r');
        }
    }
}
