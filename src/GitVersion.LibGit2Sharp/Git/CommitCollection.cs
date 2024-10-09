using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion.Git;

internal sealed class CommitCollection : ICommitCollection
{
    private readonly ICommitLog innerCollection;
    private readonly Lazy<IReadOnlyCollection<ICommit>> commits;

    internal CommitCollection(ICommitLog collection)
    {
        this.innerCollection = collection.NotNull();
        this.commits = new Lazy<IReadOnlyCollection<ICommit>>(() => this.innerCollection.Select(commit => new Commit(commit)).ToArray());
    }

    public IEnumerator<ICommit> GetEnumerator()
        => this.commits.Value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<ICommit> GetCommitsPriorTo(DateTimeOffset olderThan)
        => this.SkipWhile(c => c.When > olderThan);

    public IEnumerable<ICommit> QueryBy(CommitFilter commitFilter)
    {
        var includeReachableFrom = GetReacheableFrom(commitFilter.IncludeReachableFrom);
        var excludeReachableFrom = GetReacheableFrom(commitFilter.ExcludeReachableFrom);
        var filter = new LibGit2Sharp.CommitFilter
        {
            IncludeReachableFrom = includeReachableFrom,
            ExcludeReachableFrom = excludeReachableFrom,
            FirstParentOnly = commitFilter.FirstParentOnly,
            SortBy = (LibGit2Sharp.CommitSortStrategies)commitFilter.SortBy
        };
        var commitLog = ((IQueryableCommitLog)this.innerCollection).QueryBy(filter);
        return new CommitCollection(commitLog);

        static object? GetReacheableFrom(object? item) =>
            item switch
            {
                Commit c => (LibGit2Sharp.Commit)c,
                Branch b => (LibGit2Sharp.Branch)b,
                _ => null
            };
    }
}
