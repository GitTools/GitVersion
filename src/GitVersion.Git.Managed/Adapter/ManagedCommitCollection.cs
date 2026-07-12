using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class ManagedCommitCollection : ICommitCollection
{
    private readonly ManagedGitRepository repository;
    private readonly bool fromHead;
    private readonly IReadOnlyList<GitObjectId> include;
    private readonly IReadOnlyList<GitObjectId> exclude;
    private readonly GitRevisionSortStrategies sort;
    private readonly bool firstParentOnly;

    // Memoizes the walk result for the session it was computed against, so repeated
    // enumerations are cheap while mutations (which replace the session) stay visible.
    private (ManagedRepositorySession Session, IReadOnlyList<ICommit> Commits)? cached;

    private ManagedCommitCollection(
        ManagedGitRepository repository,
        bool fromHead,
        IReadOnlyList<GitObjectId> include,
        IReadOnlyList<GitObjectId> exclude,
        GitRevisionSortStrategies sort,
        bool firstParentOnly)
    {
        this.repository = repository.NotNull();
        this.fromHead = fromHead;
        this.include = include;
        this.exclude = exclude;
        this.sort = sort;
        this.firstParentOnly = firstParentOnly;
    }

    /// <summary>
    /// The commits reachable from HEAD in reverse chronological order, matching libgit2's
    /// default commit log. HEAD is resolved lazily at enumeration time.
    /// </summary>
    public static ManagedCommitCollection FromHead(ManagedGitRepository repository) =>
        new(repository, fromHead: true, [], [], GitRevisionSortStrategies.Time, firstParentOnly: false);

    /// <summary>
    /// The commits reachable from the given tip in reverse chronological order,
    /// matching libgit2's <c>Branch.Commits</c>.
    /// </summary>
    public static ManagedCommitCollection ReachableFrom(ManagedGitRepository repository, ManagedCommit? tip) =>
        new(repository, fromHead: false, tip is null ? [] : [tip.ObjectId], [], GitRevisionSortStrategies.Time, firstParentOnly: false);

    public IEnumerator<ICommit> GetEnumerator() => GetCommits().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<ICommit> GetCommitsPriorTo(DateTimeOffset olderThan)
        => this.SkipWhile(c => c.When > olderThan);

    public IEnumerable<ICommit> QueryBy(CommitFilter commitFilter)
    {
        var includeId = ResolveCommitish(commitFilter.IncludeReachableFrom);
        var excludeId = ResolveCommitish(commitFilter.ExcludeReachableFrom);

        return new ManagedCommitCollection(
            this.repository,
            fromHead: false,
            includeId is { } incl ? [incl] : [],
            excludeId is { } excl ? [excl] : [],
            MapSortStrategies(commitFilter.SortBy),
            commitFilter.FirstParentOnly);
    }

    private IReadOnlyList<ICommit> GetCommits()
    {
        var session = this.repository.Session;

        if (this.cached is { } memo && ReferenceEquals(memo.Session, session))
        {
            return memo.Commits;
        }

        var options = new GitRevisionWalkOptions
        {
            Sort = this.sort,
            FirstParentOnly = this.firstParentOnly
        };

        if (this.fromHead)
        {
            if (session.HeadTipId is { } headId)
            {
                options.Include.Add(headId);
            }
        }
        else
        {
            foreach (var id in this.include)
            {
                options.Include.Add(id);
            }
        }

        foreach (var id in this.exclude)
        {
            options.Exclude.Add(id);
        }

        IReadOnlyList<ICommit> commits = options.Include.Count == 0
            ? []
            : [.. session.Walker.Walk(options).Select(session.WrapCommit)];

        this.cached = (session, commits);
        return commits;
    }

    private static GitObjectId? ResolveCommitish(ICommitish? item) =>
        item switch
        {
            ManagedCommit commit => commit.ObjectId,
            ManagedBranch branch => (branch.Tip as ManagedCommit)?.ObjectId,
            _ => null
        };

    private static GitRevisionSortStrategies MapSortStrategies(CommitSortStrategies sortBy)
    {
        var result = GitRevisionSortStrategies.None;

        if (sortBy.HasFlag(CommitSortStrategies.Topological))
        {
            result |= GitRevisionSortStrategies.Topological;
        }

        if (sortBy.HasFlag(CommitSortStrategies.Time))
        {
            result |= GitRevisionSortStrategies.Time;
        }

        if (sortBy.HasFlag(CommitSortStrategies.Reverse))
        {
            result |= GitRevisionSortStrategies.Reverse;
        }

        return result;
    }
}
