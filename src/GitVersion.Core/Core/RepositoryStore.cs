using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion;

internal class RepositoryStore(ILogger<RepositoryStore> logger, IGitRepository repository, BranchResolver? branchResolver = null)
    : IRepositoryStore, ICachedCommitLogProvider
{
    private readonly ILogger<RepositoryStore> logger = logger.NotNull();
    private readonly IGitRepository repository = repository.NotNull();
    private readonly Dictionary<string, CommitGraph> commitGraphCache = [];
    private readonly Dictionary<IgnoredCommits, CommitMask> ignoredMaskCache = [];
    private readonly Dictionary<ExcludedCommits, CommitMask> excludedMaskCache = [];

    public int UncommittedChangesCount => this.repository.UncommittedChangesCount();

    public IBranch Head => this.repository.Head;

    private ContextualBranch? contextualBranch;
    private ContextualBranch? ContextualBranch => branchResolver?.ContextBranch is { } name
        ? this.contextualBranch ??= new ContextualBranch(name, Head)
        : null;

    public IBranchCollection Branches => ContextualBranch is { } context
        ? new ContextualBranchCollection(this.repository.Branches, context)
        : this.repository.Branches;

    public ITagCollection Tags => this.repository.Tags;

    /// <summary>
    ///     Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
    /// </summary>
    public ICommit? FindMergeBase(IBranch? branch, IBranch? otherBranch)
    {
        var mergeBaseFinder = new MergeBaseFinder(this, this.logger);
        return mergeBaseFinder.FindMergeBaseOf(branch, otherBranch);
    }

    public ICommit? FindMergeBase(ICommit commit, ICommit mainlineTip) => this.repository.FindMergeBase(commit, mainlineTip);

    public ICommit? GetCurrentCommit(IBranch currentBranch, string? commitId, IIgnoreConfiguration ignore)
    {
        currentBranch.NotNull();
        ignore.NotNull();

        ICommit? currentCommit = null;
        if (!commitId.IsNullOrWhiteSpace())
        {
            this.logger.LogInformation("Searching for specific commit '{CommitId}'", commitId);

            var commit = this.repository.Commits.FirstOrDefault(c => string.Equals(c.Sha, commitId, StringComparison.OrdinalIgnoreCase));
            if (commit != null)
            {
                currentCommit = commit;
            }
            else
            {
                this.logger.LogWarning("Commit '{CommitId}' specified but not found", commitId);
            }
        }

        IEnumerable<ICommit> commits = currentBranch.Commits;
        if (currentCommit != null)
        {
            commits = currentBranch.Commits.GetCommitsPriorTo(currentCommit.When);
        }
        else
        {
            this.logger.LogInformation("Using latest commit on specified branch");
        }

        commits = ignore.Filter(commits);
        return commits.FirstOrDefault();
    }

    public IBranch GetTargetBranch(string? targetBranchName)
    {
        if (ContextualBranch is { } context)
        {
            return context;
        }
        // By default, we assume HEAD is pointing to the desired branch
        var desiredBranch = this.repository.Head;

        // Make sure the desired branch has been specified
        if (targetBranchName.IsNullOrEmpty())
        {
            return desiredBranch;
        }

        // There are some edge cases where HEAD is not pointing to the desired branch.
        // Therefore, it's important to verify if 'currentBranch' is indeed the desired branch.
        var targetBranch = FindBranch(targetBranchName);

        // CanonicalName can be "refs/heads/develop", so we need to check for "/{TargetBranch}" as well
        if (desiredBranch.Equals(targetBranch))
        {
            return desiredBranch;
        }

        // In the case where HEAD is not the desired branch, try to find the branch with matching name
        desiredBranch = Branches.Where(b => b.Name.EquivalentTo(targetBranchName)).MinBy(b => b.IsRemote);

        // Failsafe in case the specified branch is invalid
        desiredBranch ??= this.repository.Head;

        return desiredBranch;
    }

    public IBranch? FindBranch(ReferenceName branchName) => Branches.FirstOrDefault(x => x.Name.Equals(branchName));

    public IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude) => Branches.ExcludeBranches(branchesToExclude);

    public IEnumerable<IBranch> GetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false)
    {
        commit.NotNull();

        var branchesContainingCommitFinder = new BranchesContainingCommitFinder(this, this.logger);
        return branchesContainingCommitFinder.GetBranchesContainingCommit(commit, branches, onlyTrackedBranches);
    }

    public IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration,
            params IBranch[] excludedBranches)
        => GetSourceBranches(branch, configuration, (IEnumerable<IBranch>)excludedBranches);

    public IEnumerable<IBranch> GetSourceBranches(
        IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches)
    {
        var returnedBranches = new HashSet<IBranch>();

        var referenceLookup = this.repository.References.ToLookup(r => r.TargetIdentifier);

        var commitBranches = FindCommitBranchesBranchedFrom(
            branch, configuration, excludedBranches, excludeIgnoredBranches: false).ToHashSet();

        var ignore = CollectIgnoredMergeCommitBranches(branch, commitBranches);

        RemoveCommitBranchesFoundInOtherBranches(commitBranches, ignore);

        foreach (var branchGrouping in commitBranches.GroupBy(element => element.Commit, element => element.Branch))
        {
            foreach (var item in GetSourceBranchesForGrouping(branchGrouping, referenceLookup, returnedBranches))
            {
                yield return item;
            }
        }
    }

    private static HashSet<BranchCommit> CollectIgnoredMergeCommitBranches(IBranch branch, HashSet<BranchCommit> commitBranches)
    {
        var ignore = new HashSet<BranchCommit>();
        foreach (var commitBranch in commitBranches)
        {
            foreach (var commit in branch.Commits.Where(element => element.When > commitBranch.Commit.When))
            {
                var parents = commit.Parents.ToArray();
                if (parents.Length > 1 && parents.Any(element => element.Equals(commitBranch.Commit)))
                {
                    ignore.Add(commitBranch);
                }
            }
        }

        return ignore;
    }

    private static void RemoveCommitBranchesFoundInOtherBranches(HashSet<BranchCommit> commitBranches, HashSet<BranchCommit> ignore)
    {
        foreach (var item in commitBranches.Skip(1).Reverse().Where(item => !ignore.Contains(item)))
        {
            foreach (var commitBranch in commitBranches)
            {
                if (item.Commit.Equals(commitBranch.Commit))
                {
                    break;
                }

                if (commitBranch.Branch.Commits.Any(element => element.When >= item.Commit.When && element.Equals(item.Commit)))
                {
                    commitBranches.Remove(item);
                }
            }
        }
    }

    private static IEnumerable<IBranch> GetSourceBranchesForGrouping(
        IGrouping<ICommit, IBranch> branchGrouping, ILookup<string, IReference> referenceLookup, HashSet<IBranch> returnedBranches)
    {
        var referenceMatchFound = false;
        var referenceNames = referenceLookup[branchGrouping.Key.Sha].Select(element => element.Name).ToHashSet();

        foreach (var item in branchGrouping.Where(item => referenceNames.Contains(item.Name)))
        {
            if (returnedBranches.Add(item))
            {
                yield return item;
            }

            referenceMatchFound = true;
        }

        if (referenceMatchFound)
        {
            yield break;
        }

        foreach (var item in branchGrouping.Where(returnedBranches.Add))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Find the commit where the given branch was branched from another branch.
    ///     If there are multiple such commits and branches, tries to guess based on commit histories.
    /// </summary>
    public BranchCommit FindCommitBranchBranchedFrom(IBranch? branch, IGitVersionConfiguration configuration,
        params IBranch[] excludedBranches)
    {
        branch = branch.NotNull();

        using (this.logger.StartIndentedScope($"Finding branch source of '{branch}'"))
        {
            if (branch.Tip == null)
            {
                this.logger.LogWarning("Branch {Branch} has no tip.", branch);
                return BranchCommit.Empty;
            }

            var possibleBranches =
                new MergeCommitFinder(this, configuration, excludedBranches, this.logger, excludeIgnoredBranches: true)
                    .FindMergeCommitsFor(branch)
                    .ToList();

            if (possibleBranches.Count <= 1)
            {
                return possibleBranches.SingleOrDefault();
            }

            var first = possibleBranches[0];
            this.logger.LogInformation(
                """
                Multiple source branches have been found, picking the first one ({Branch}).
                This may result in incorrect commit counting.
                Options were:
                {Options}
                """,
                first.Branch,
                string.Join(", ", possibleBranches.Select(b => b.Branch.ToString())));
            return first;
        }
    }

    public IEnumerable<BranchCommit> FindCommitBranchesBranchedFrom(
            IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches)
        => FindCommitBranchesBranchedFrom(
            branch, configuration, excludedBranches, excludeIgnoredBranches: true);

    /// <summary>
    /// Returns the commits reachable from <paramref name="currentCommit"/> which are not reachable from
    /// <paramref name="baseVersionSource"/>, in the order the revision walk emits them.
    /// </summary>
    public IReadOnlyList<ICommit> GetCommitLog(ICommit? baseVersionSource, ICommit currentCommit, IIgnoreConfiguration ignore)
    {
        currentCommit.NotNull();
        ignore.NotNull();

        var filter = new CommitFilter
        {
            IncludeReachableFrom = currentCommit,
            ExcludeReachableFrom = baseVersionSource,
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
        };

        var commits = FilterCommits(filter).ToArray();
        return [.. ignore.Filter(commits)];
    }

    /// <inheritdoc />
    public IReadOnlyList<ICommit> GetCommitLog(
        ICommit? baseVersionSource, ICommit currentCommit, IIgnoreConfiguration ignore, IReadOnlySet<string> excludedShas)
    {
        currentCommit.NotNull();
        ignore.NotNull();
        excludedShas.NotNull();

        var graph = GetCommitGraph(currentCommit);
        var ignored = GetIgnoredMask(graph, currentCommit, ignore);
        var excluded = GetExcludedMask(graph, currentCommit, ignore, ignored, excludedShas);

        return graph.GetCommits(baseVersionSource, ignored, excluded);
    }

    /// <summary>
    /// Returns the commits reachable from <paramref name="currentCommit"/>. The walk is performed once and
    /// cached, because the reachable history cannot change while a single version is calculated.
    /// </summary>
    private CommitGraph GetCommitGraph(ICommit currentCommit)
        => this.commitGraphCache.GetOrAdd(currentCommit.Sha, () =>
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = currentCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            return CommitGraph.Create([.. FilterCommits(filter)]);
        });

    /// <summary>Returns the commits which <paramref name="ignore"/> removes from the history.</summary>
    private CommitMask GetIgnoredMask(CommitGraph graph, ICommit currentCommit, IIgnoreConfiguration ignore)
        => ignore.IsEmpty
            ? CommitMask.Empty
            : this.ignoredMaskCache.GetOrAdd(new(currentCommit.Sha, ignore), () => graph.CreateIgnoredMask(ignore));

    /// <summary>
    /// Returns the commits which <paramref name="excludedShas"/> removes from the history, which are those commits
    /// together with everything they build upon. Which commits those are does not depend on the base version
    /// source, so the result is cached once per set of excluded commits instead of once per requested commit log.
    /// </summary>
    private CommitMask GetExcludedMask(
        CommitGraph graph, ICommit currentCommit, IIgnoreConfiguration ignore, CommitMask ignored, IReadOnlySet<string> excludedShas)
        => excludedShas.Count == 0
            ? CommitMask.Empty
            : this.excludedMaskCache.GetOrAdd(
                new(currentCommit.Sha, ignore, excludedShas), () => graph.CreateAncestorMask(excludedShas, ignored));

    public IReadOnlyList<ICommit> GetCommitsReacheableFromHead(ICommit? headCommit, IIgnoreConfiguration ignore)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = headCommit,
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
        };

        var commits = FilterCommits(filter).ToArray();
        return [.. ignore.Filter(commits)];
    }

    public IReadOnlyList<ICommit> GetCommitsReacheableFrom(ICommit commit, IBranch branch)
    {
        var filter = new CommitFilter { IncludeReachableFrom = branch };

        var commits = FilterCommits(filter);
        return [.. commits.Where(c => c.Sha == commit.Sha)];
    }

    public ICommit? GetForwardMerge(ICommit? commitToFindCommonBase, ICommit? findMergeBase)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = commitToFindCommonBase,
            ExcludeReachableFrom = findMergeBase
        };

        var commits = FilterCommits(filter);
        return commits.FirstOrDefault(c => c.Parents.Contains(findMergeBase));
    }

    public bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit)
    {
        var filter = new CommitFilter { IncludeReachableFrom = branch, ExcludeReachableFrom = baseVersionSource, FirstParentOnly = true };
        var commits = FilterCommits(filter);
        return commits.Contains(firstMatchingCommit);
    }

    private IEnumerable<ICommit> FilterCommits(CommitFilter filter) => this.repository.Commits.QueryBy(filter);

    private IBranch? FindBranch(string branchName) => Branches.FirstOrDefault(x => x.Name.EquivalentTo(branchName));

    private List<BranchCommit> FindCommitBranchesBranchedFrom(
        IBranch branch,
        IGitVersionConfiguration configuration,
        IEnumerable<IBranch> excludedBranches,
        bool excludeIgnoredBranches)
    {
        using (this.logger.StartIndentedScope($"Finding branches source of '{branch}'"))
        {
            if (branch.Tip != null)
            {
                return [.. new MergeCommitFinder(
                    this, configuration, excludedBranches, this.logger, excludeIgnoredBranches).FindMergeCommitsFor(branch)];
            }

            this.logger.LogWarning("Branch {Branch} has no tip.", branch);
            return [];
        }
    }

    /// <summary>Identifies the commits removed by one set of ignore rules from the history of one head commit.</summary>
    private readonly record struct IgnoredCommits(string HeadSha, IIgnoreConfiguration Ignore);

    /// <summary>
    /// Identifies the commits removed by one set of excluded commits from the history of one head commit. The
    /// excluded set takes part in the identity by reference, which is why callers must not modify it afterwards.
    /// </summary>
    private readonly record struct ExcludedCommits(string HeadSha, IIgnoreConfiguration Ignore, IReadOnlySet<string> ExcludedShas);
}
