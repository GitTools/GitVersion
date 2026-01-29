using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion;

internal class MergeBaseFinder(IRepositoryStore repositoryStore, ILogger logger)
{
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly ILogger logger = logger.NotNull();
    private readonly Dictionary<Tuple<IBranch, IBranch>, ICommit> mergeBaseCache = [];

    public ICommit? FindMergeBaseOf(IBranch? first, IBranch? second)
    {
        first = first.NotNull();
        second = second.NotNull();

        var key = Tuple.Create(first, second);

        if (this.mergeBaseCache.TryGetValue(key, out var mergeBase))
        {
            this.logger.LogDebug("Cache hit for merge base between '{First}' and '{Second}'.", first, second);
            return mergeBase;
        }

        using (this.logger.StartIndentedScope($"Finding merge base between '{first}' and '{second}'."))
        {
            // Other branch tip is a forward merge
            var commitToFindCommonBase = second.Tip;
            var commit = first.Tip;

            if (commit == null)
                return null;

            if (commitToFindCommonBase?.Parents.Contains(commit) == true)
            {
                commitToFindCommonBase = commitToFindCommonBase.Parents[0];
            }

            if (commitToFindCommonBase == null)
                return null;

            var findMergeBase = FindMergeBase(commit, commitToFindCommonBase);

            if (findMergeBase == null)
            {
                this.logger.LogInformation("No merge base of '{First}' and '{Second}' could be found.", first, second);
                return null;
            }

            // Store in cache.
            this.mergeBaseCache.Add(key, findMergeBase);

            this.logger.LogInformation("Merge base of '{First}' and '{Second}' is '{MergeBase}'", first, second, findMergeBase);
            return findMergeBase;
        }
    }

    private ICommit? FindMergeBase(ICommit commit, ICommit commitToFindCommonBase)
    {
        var findMergeBase = this.repositoryStore.FindMergeBase(commit, commitToFindCommonBase);
        if (findMergeBase == null)
            return null;

        this.logger.LogInformation("Found merge base of '{MergeBase}'", findMergeBase);

        // We do not want to include merge base commits which got forward merged into the other branch
        ICommit? forwardMerge;
        do
        {
            // Now make sure that the merge base is not a forward merge
            forwardMerge = this.repositoryStore.GetForwardMerge(commitToFindCommonBase, findMergeBase);

            if (forwardMerge == null)
                continue;

            // TODO Fix the logging up in this section
            var second = forwardMerge.Parents[0];
            this.logger.LogDebug("Second {Second}", second);
            var mergeBase = this.repositoryStore.FindMergeBase(commit, second);
            if (mergeBase == null)
            {
                this.logger.LogWarning("Could not find merge base for {Commit}", commit);
            }
            else
            {
                this.logger.LogDebug("New Merge base {MergeBase}", mergeBase);
            }

            if (Equals(mergeBase, findMergeBase))
            {
                this.logger.LogDebug("Breaking");
                break;
            }

            findMergeBase = mergeBase;
            commitToFindCommonBase = second;
            this.logger.LogInformation("next merge base --> {MergeBase}", findMergeBase);
        } while (forwardMerge != null);

        return findMergeBase;
    }
}
