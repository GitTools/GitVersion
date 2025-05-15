using System.Collections.Concurrent;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class Commit : GitObject, ICommit
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<string>> pathsCache = new();
    private static readonly LambdaEqualityHelper<ICommit> equalityHelper = new(x => x.Id);
    private static readonly LambdaKeyComparer<ICommit, string> comparerHelper = new(x => x.Sha);
    private readonly Lazy<IReadOnlyList<ICommit>> parentsLazy;

    private readonly LibGit2Sharp.Commit innerCommit;
    private readonly LibGit2Sharp.Diff repoDiff;

    internal Commit(LibGit2Sharp.Commit innerCommit, LibGit2Sharp.Diff repoDiff) : base(innerCommit)
    {
        this.innerCommit = innerCommit.NotNull();
        this.parentsLazy = new(() => innerCommit.Parents.Select(parent => new Commit(parent, repoDiff)).ToList());
        When = innerCommit.Committer.When;
        this.repoDiff = repoDiff;
    }

    public int CompareTo(ICommit? other) => comparerHelper.Compare(this, other);
    public bool Equals(ICommit? other) => equalityHelper.Equals(this, other);
    public IReadOnlyList<ICommit> Parents => this.parentsLazy.Value;
    public DateTimeOffset When { get; }
    public string Message => this.innerCommit.Message;
    // TODO implement tag prefix filtering before returning the paths.
    public IEnumerable<string> DiffPaths
    {
        get
        {
            if (pathsCache.TryGetValue(this.Sha, out var paths))
                return paths;
            else
                return this.CommitChanges?.Paths ?? [];
        }
    }
    public override bool Equals(object? obj) => Equals(obj as ICommit);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => $"'{Id.ToString(7)}' - {this.innerCommit.MessageShort}";
    public static implicit operator LibGit2Sharp.Commit(Commit d) => d.innerCommit;
    private ITreeChanges CommitChanges => new TreeChanges(this.repoDiff.Compare<LibGit2Sharp.TreeChanges>(this.innerCommit.Tree, this.innerCommit.Parents.FirstOrDefault()?.Tree));
}
