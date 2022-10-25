using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion;

internal class MergeBaseFinder
{
    private readonly ILog log;
    private readonly Dictionary<Tuple<IBranch, IBranch>, ICommit> mergeBaseCache = new();
    private readonly IGitRepository repository;
    private readonly IRepositoryStore repositoryStore;

    public MergeBaseFinder(IRepositoryStore repositoryStore, IGitRepository gitRepository, ILog log)
    {
        this.repositoryStore = repositoryStore.NotNull();
        this.repository = gitRepository.NotNull();
        this.log = log.NotNull();
    }

    public ICommit? FindMergeBaseOf(IBranch? first, IBranch? second)
    {
        first = first.NotNull();
        second = second.NotNull();

        var key = Tuple.Create(first, second);

        if (this.mergeBaseCache.ContainsKey(key))
        {
            this.log.Debug($"Cache hit for merge base between '{first}' and '{second}'.");
            return this.mergeBaseCache[key];
        }

        using (this.log.IndentLog($"Finding merge base between '{first}' and '{second}'."))
        {
            // Other branch tip is a forward merge
            var commitToFindCommonBase = second?.Tip;
            var commit = first.Tip;

            if (commit == null)
                return null;

            if (commitToFindCommonBase?.Parents.Contains(commit) == true)
            {
                commitToFindCommonBase = commitToFindCommonBase.Parents.First();
            }

            if (commitToFindCommonBase == null)
                return null;

            var findMergeBase = FindMergeBase(commit, commitToFindCommonBase);

            if (findMergeBase == null)
            {
                this.log.Info($"No merge base of {first}' and '{second} could be found.");
                return null;
            }

            // Store in cache.
            this.mergeBaseCache.Add(key, findMergeBase);

            this.log.Info($"Merge base of {first}' and '{second} is {findMergeBase}");
            return findMergeBase;
        }
    }

    private ICommit? FindMergeBase(ICommit commit, ICommit commitToFindCommonBase)
    {
        var findMergeBase = this.repositoryStore.FindMergeBase(commit, commitToFindCommonBase);
        if (findMergeBase == null)
            return null;

        this.log.Info($"Found merge base of {findMergeBase}");

        // We do not want to include merge base commits which got forward merged into the other branch
        ICommit? forwardMerge;
        do
        {
            // Now make sure that the merge base is not a forward merge
            forwardMerge = GetForwardMerge(commitToFindCommonBase, findMergeBase);

            if (forwardMerge == null)
                continue;

            // TODO Fix the logging up in this section
            var second = forwardMerge.Parents.First();
            this.log.Debug($"Second {second}");
            var mergeBase = this.repositoryStore.FindMergeBase(commit, second);
            if (mergeBase == null)
            {
                this.log.Warning("Could not find merge base for " + commit);
            }
            else
            {
                this.log.Debug($"New Merge base {mergeBase}");
            }

            if (Equals(mergeBase, findMergeBase))
            {
                this.log.Debug("Breaking");
                break;
            }

            findMergeBase = mergeBase;
            commitToFindCommonBase = second;
            this.log.Info($"Merge base was due to a forward merge, next merge base is {findMergeBase}");
        } while (forwardMerge != null);

        return findMergeBase;
    }

    private ICommit? GetForwardMerge(ICommit? commitToFindCommonBase, ICommit? findMergeBase)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = commitToFindCommonBase,
            ExcludeReachableFrom = findMergeBase
        };
        var commitCollection = this.repository.Commits.QueryBy(filter);

        return commitCollection.FirstOrDefault(c => c.Parents.Contains(findMergeBase));
    }
}
