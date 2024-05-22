using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion;

internal class RepositoryStore : IRepositoryStore
{
    private readonly ILog log;
    private readonly IGitRepository repository;

    private readonly MergeBaseFinder mergeBaseFinder;

    public RepositoryStore(ILog log, IGitRepository repository)
    {
        this.log = log.NotNull();
        this.repository = repository.NotNull();
        this.mergeBaseFinder = new(this, repository, log);
    }

    /// <summary>
    ///     Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
    /// </summary>
    public ICommit? FindMergeBase(IBranch? branch, IBranch? otherBranch)
        => this.mergeBaseFinder.FindMergeBaseOf(branch, otherBranch);

    public ICommit? GetCurrentCommit(IBranch currentBranch, string? commitId, IIgnoreConfiguration ignore)
    {
        currentBranch.NotNull();
        ignore.NotNull();

        ICommit? currentCommit = null;
        if (!commitId.IsNullOrWhiteSpace())
        {
            this.log.Info($"Searching for specific commit '{commitId}'");

            var commit = this.repository.Commits.FirstOrDefault(c => string.Equals(c.Sha, commitId, StringComparison.OrdinalIgnoreCase));
            if (commit != null)
            {
                currentCommit = commit;
            }
            else
            {
                this.log.Warning($"Commit '{commitId}' specified but not found");
            }
        }

        IEnumerable<ICommit> commits = currentBranch.Commits;
        if (currentCommit != null)
        {
            commits = currentBranch.Commits.GetCommitsPriorTo(currentCommit.When);
        }
        else
        {
            this.log.Info("Using latest commit on specified branch");
        }

        commits = ignore.Filter(commits);
        return commits.FirstOrDefault();
    }

    public IBranch GetTargetBranch(string? targetBranchName)
    {
        // By default, we assume HEAD is pointing to the desired branch
        var desiredBranch = this.repository.Head;

        // Make sure the desired branch has been specified
        if (targetBranchName.IsNullOrEmpty())
            return desiredBranch;

        // There are some edge cases where HEAD is not pointing to the desired branch.
        // Therefore, it's important to verify if 'currentBranch' is indeed the desired branch.
        var targetBranch = FindBranch(targetBranchName);

        // CanonicalName can be "refs/heads/develop", so we need to check for "/{TargetBranch}" as well
        if (desiredBranch.Equals(targetBranch))
            return desiredBranch;

        // In the case where HEAD is not the desired branch, try to find the branch with matching name
        desiredBranch = this.repository.Branches.Where(b => b.Name.EquivalentTo(targetBranchName)).MinBy(b => b.IsRemote);

        // Failsafe in case the specified branch is invalid
        desiredBranch ??= this.repository.Head;

        return desiredBranch;
    }

    public IBranch? FindBranch(ReferenceName branchName) => this.repository.Branches.FirstOrDefault(x => x.Name.Equals(branchName));

    public IBranch? FindBranch(string branchName) => this.repository.Branches.FirstOrDefault(x => x.Name.EquivalentTo(branchName));

    public IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude) => this.repository.Branches.ExcludeBranches(branchesToExclude);

    public IEnumerable<IBranch> GetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false)
    {
        commit.NotNull();

        var branchesContainingCommitFinder = new BranchesContainingCommitFinder(this.repository, this.log);
        return branchesContainingCommitFinder.GetBranchesContainingCommit(commit, branches, onlyTrackedBranches);
    }

    public IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration,
            params IBranch[] excludedBranches)
        => GetSourceBranches(branch, configuration, (IEnumerable<IBranch>)excludedBranches);

    public IEnumerable<IBranch> GetSourceBranches(
        IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches)
    {
        var returnedBranches = new HashSet<IBranch>();

        var referenceLookup = this.repository.Refs.ToLookup(r => r.TargetIdentifier);

        var commitBranches = FindCommitBranchesBranchedFrom(branch, configuration, excludedBranches).ToHashSet();

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

        foreach (var item in commitBranches.Skip(1).Reverse())
        {
            if (ignore.Contains(item)) continue;

            foreach (var commitBranch in commitBranches)
            {
                if (item.Commit.Equals(commitBranch.Commit)) break;

                foreach (var commit in commitBranch.Branch.Commits.Where(element => element.When >= item.Commit.When))
                {
                    if (commit.Equals(item.Commit))
                    {
                        commitBranches.Remove(item);
                    }
                }
            }
        }

        foreach (var branchGrouping in commitBranches.GroupBy(element => element.Commit, element => element.Branch))
        {
            bool referenceMatchFound = false;
            var referenceNames = referenceLookup[branchGrouping.Key.Sha].Select(element => element.Name).ToHashSet();

            foreach (var item in branchGrouping)
            {
                if (referenceNames.Contains(item.Name))
                {
                    if (returnedBranches.Add(item)) yield return item;
                    referenceMatchFound = true;
                }
            }

            if (!referenceMatchFound)
            {
                foreach (var item in branchGrouping)
                {
                    if (returnedBranches.Add(item)) yield return item;
                }
            }
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

        using (this.log.IndentLog($"Finding branch source of '{branch}'"))
        {
            if (branch.Tip == null)
            {
                this.log.Warning($"{branch} has no tip.");
                return BranchCommit.Empty;
            }

            var possibleBranches =
                new MergeCommitFinder(this, configuration, excludedBranches, this.log)
                    .FindMergeCommitsFor(branch)
                    .ToList();

            if (possibleBranches.Count <= 1)
                return possibleBranches.SingleOrDefault();

            var first = possibleBranches[0];
            this.log.Info($"Multiple source branches have been found, picking the first one ({first.Branch}).{PathHelper.NewLine}" +
                          $"This may result in incorrect commit counting.{PathHelper.NewLine}Options were:{PathHelper.NewLine}" +
                          string.Join(", ", possibleBranches.Select(b => b.Branch.ToString())));
            return first;
        }
    }

    public IEnumerable<BranchCommit> FindCommitBranchesBranchedFrom(
            IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches)
        => FindCommitBranchesBranchedFrom(branch, configuration, (IEnumerable<IBranch>)excludedBranches);

    public IEnumerable<BranchCommit> FindCommitBranchesBranchedFrom(
        IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches)
    {
        using (this.log.IndentLog($"Finding branches source of '{branch}'"))
        {
            if (branch.Tip == null)
            {
                this.log.Warning($"{branch} has no tip.");
                return [];
            }

            return new MergeCommitFinder(this, configuration, excludedBranches, this.log).FindMergeCommitsFor(branch).ToList();
        }
    }

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

        var commits = this.repository.Commits.QueryBy(filter).ToArray();

        return ignore.Filter(commits).ToList();
    }

    public bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit)
    {
        var filter = new CommitFilter { IncludeReachableFrom = branch, ExcludeReachableFrom = baseVersionSource, FirstParentOnly = true };
        var commitCollection = this.repository.Commits.QueryBy(filter);
        return commitCollection.Contains(firstMatchingCommit);
    }

    public ICommit? FindMergeBase(ICommit commit, ICommit mainlineTip) => this.repository.FindMergeBase(commit, mainlineTip);

    public int GetNumberOfUncommittedChanges() => this.repository.GetNumberOfUncommittedChanges();
}
