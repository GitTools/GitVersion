using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion;

internal sealed class CommitCollection : ICommitCollection
{
    private readonly ICommitLog innerCollection;
    internal CommitCollection(ICommitLog collection) => this.innerCollection = collection.NotNull();

    public IEnumerator<ICommit> GetEnumerator() => this.innerCollection.Select(commit => new Commit(commit)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<ICommit> GetCommitsPriorTo(DateTimeOffset olderThan) => this.SkipWhile(c => c.When > olderThan);

    public IEnumerable<ICommit> QueryBy(CommitFilter commitFilter)
    {
        static object? GetReachableFrom(object? item) =>
            item switch
            {
                Commit c => (LibGit2Sharp.Commit)c,
                Branch b => (LibGit2Sharp.Branch)b,
                _ => null
            };

        var includeReachableFrom = GetReachableFrom(commitFilter.IncludeReachableFrom);
        var excludeReachableFrom = GetReachableFrom(commitFilter.ExcludeReachableFrom);
        var filter = new LibGit2Sharp.CommitFilter
        {
            IncludeReachableFrom = includeReachableFrom,
            ExcludeReachableFrom = excludeReachableFrom,
            FirstParentOnly = commitFilter.FirstParentOnly,
            SortBy = (LibGit2Sharp.CommitSortStrategies)commitFilter.SortBy
        };
        var commitLog = ((IQueryableCommitLog)this.innerCollection).QueryBy(filter);
        return new CommitCollection(commitLog);
    }
}
