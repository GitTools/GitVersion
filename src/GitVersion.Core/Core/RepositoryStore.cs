using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion;

public class RepositoryStore : IRepositoryStore
{
    private readonly ILog log;
    private readonly IGitRepository repository;
    private readonly Dictionary<IBranch, List<SemanticVersion>> semanticVersionTagsOnBranchCache = new();
    private readonly MergeBaseFinder mergeBaseFinder;

    public RepositoryStore(ILog log, IGitRepository repository)
    {
        this.log = log.NotNull();
        this.repository = repository.NotNull();
        this.mergeBaseFinder = new MergeBaseFinder(this, repository, log);
    }

    /// <summary>
    ///     Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
    /// </summary>
    public ICommit? FindMergeBase(IBranch? branch, IBranch? otherBranch)
        => this.mergeBaseFinder.FindMergeBaseOf(branch, otherBranch);

    public ICommit? GetCurrentCommit(IBranch currentBranch, string? commitId)
    {
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

        if (currentCommit != null)
            return currentCommit;

        this.log.Info("Using latest commit on specified branch");
        currentCommit = currentBranch.Tip;

        return currentCommit;
    }

    public IEnumerable<ICommit> GetMainlineCommitLog(ICommit? baseVersionSource, ICommit? mainlineTip)
    {
        if (mainlineTip is null)
        {
            return Enumerable.Empty<ICommit>();
        }

        var filter = new CommitFilter { IncludeReachableFrom = mainlineTip, ExcludeReachableFrom = baseVersionSource, SortBy = CommitSortStrategies.Reverse, FirstParentOnly = true };

        return this.repository.Commits.QueryBy(filter);
    }

    public IEnumerable<ICommit> GetMergeBaseCommits(ICommit? mergeCommit, ICommit? mergedHead, ICommit? findMergeBase)
    {
        var filter = new CommitFilter { IncludeReachableFrom = mergedHead, ExcludeReachableFrom = findMergeBase };
        var commitCollection = this.repository.Commits.QueryBy(filter);

        var commits = mergeCommit != null
            ? new[] { mergeCommit }.Union(commitCollection)
            : commitCollection;
        return commits;
    }

    public IBranch GetTargetBranch(string? targetBranchName)
    {
        // By default, we assume HEAD is pointing to the desired branch
        var desiredBranch = this.repository.Head;

        // Make sure the desired branch has been specified
        if (targetBranchName.IsNullOrEmpty())
            return desiredBranch;

        // There are some edge cases where HEAD is not pointing to the desired branch.
        // Therefore it's important to verify if 'currentBranch' is indeed the desired branch.
        var targetBranch = FindBranch(targetBranchName);

        // CanonicalName can be "refs/heads/develop", so we need to check for "/{TargetBranch}" as well
        if (desiredBranch.Equals(targetBranch))
            return desiredBranch;

        // In the case where HEAD is not the desired branch, try to find the branch with matching name
        desiredBranch = this.repository.Branches
            .Where(b => b.Name.EquivalentTo(targetBranchName))
            .OrderBy(b => b.IsRemote)
            .FirstOrDefault();

        // Failsafe in case the specified branch is invalid
        desiredBranch ??= this.repository.Head;

        return desiredBranch;
    }

    public IBranch? FindBranch(string? branchName) => this.repository.Branches.FirstOrDefault(x => x.Name.EquivalentTo(branchName));

    public IBranch? FindMainBranch(GitVersionConfiguration configuration)
    {
        var mainBranchRegex = configuration.Branches[GitVersionConfiguration.MainBranchKey]?.Regex
                              ?? configuration.Branches[GitVersionConfiguration.MasterBranchKey]?.Regex;

        if (mainBranchRegex == null)
        {
            return FindBranch(GitVersionConfiguration.MainBranchKey) ?? FindBranch(GitVersionConfiguration.MasterBranchKey);
        }

        return this.repository.Branches.FirstOrDefault(b =>
            Regex.IsMatch(b.Name.Friendly, mainBranchRegex, RegexOptions.IgnoreCase));
    }

    public IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, BranchConfiguration>> releaseBranchConfig)
        => this.repository.Branches.Where(b => IsReleaseBranch(b, releaseBranchConfig));

    public IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude) => this.repository.Branches.ExcludeBranches(branchesToExclude);

    public IEnumerable<IBranch> GetBranchesContainingCommit(ICommit? commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false)
    {
        var branchesContainingCommitFinder = new BranchesContainingCommitFinder(this.repository, this.log);
        return branchesContainingCommitFinder.GetBranchesContainingCommit(commit, branches, onlyTrackedBranches);
    }

    public IDictionary<string, List<IBranch>> GetMainlineBranches(ICommit commit, GitVersionConfiguration configuration)
    {
        var mainlineBranchFinder = new MainlineBranchFinder(this, this.repository, configuration, this.log);
        return mainlineBranchFinder.FindMainlineBranches(commit);
    }

    public IEnumerable<IBranch> GetSourceBranches(IBranch branch, GitVersionConfiguration configuration, params IBranch[] excludedBranches)
        => GetSourceBranches(branch, configuration, (IEnumerable<IBranch>)excludedBranches);

    public IEnumerable<IBranch> GetSourceBranches(IBranch branch, GitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches)
    {
        var referenceLookup = this.repository.Refs.ToLookup(r => r.TargetIdentifier);

        var returnedBranches = new HashSet<IBranch>();
        if (referenceLookup.Any())
        {
            foreach (var branchCommit in FindCommitBranchesWasBranchedFrom(branch, configuration, excludedBranches))
            {
                foreach (var item in referenceLookup[branchCommit.Commit.Sha]
                    .Where(r => r.Name.Friendly == branchCommit.Branch.Name.Friendly))
                {
                    if (returnedBranches.Add(branchCommit.Branch))
                    {
                        yield return branchCommit.Branch;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Find the commit where the given branch was branched from another branch.
    ///     If there are multiple such commits and branches, tries to guess based on commit histories.
    /// </summary>
    public BranchCommit FindCommitBranchWasBranchedFrom(IBranch? branch, GitVersionConfiguration configuration, params IBranch[] excludedBranches)
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

            var first = possibleBranches.First();
            this.log.Info($"Multiple source branches have been found, picking the first one ({first.Branch}).{System.Environment.NewLine}" +
                          $"This may result in incorrect commit counting.{System.Environment.NewLine}Options were:{System.Environment.NewLine}" +
                          string.Join(", ", possibleBranches.Select(b => b.Branch.ToString())));
            return first;
        }
    }

    public IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, GitVersionConfiguration configuration, params IBranch[] excludedBranches)
        => FindCommitBranchesWasBranchedFrom(branch, configuration, (IEnumerable<IBranch>)excludedBranches);

    public IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, GitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches)
    {
        using (this.log.IndentLog($"Finding branches source of '{branch}'"))
        {
            if (branch.Tip == null)
            {
                this.log.Warning($"{branch} has no tip.");
                yield break;
            }

            DateTimeOffset? when = null;
            var branchCommits = new MergeCommitFinder(this, configuration, excludedBranches, this.log)
                .FindMergeCommitsFor(branch).ToList();
            foreach (var branchCommit in branchCommits)
            {
                if (when != null && branchCommit.Commit.When != when) break;
                yield return branchCommit;
                when = branchCommit.Commit.When;
            }
        }
    }

    public SemanticVersion? GetCurrentCommitTaggedVersion(ICommit? commit, string? tagPrefix)
        => this.repository.Tags
            .SelectMany(t => GetCurrentCommitSemanticVersions(commit, tagPrefix, t))
            .Max();

    public IEnumerable<SemanticVersion> GetVersionTagsOnBranch(IBranch branch, string? tagPrefixRegex)
    {
        branch = branch.NotNull();

        if (this.semanticVersionTagsOnBranchCache.ContainsKey(branch))
        {
            this.log.Debug($"Cache hit for version tags on branch '{branch.Name.Canonical}");
            return this.semanticVersionTagsOnBranchCache[branch];
        }

        using (this.log.IndentLog($"Getting version tags from branch '{branch.Name.Canonical}'."))
        {
            var tags = GetValidVersionTags(tagPrefixRegex);
            var tagsBySha = tags.Where(t => t.Tag.TargetSha != null).ToLookup(t => t.Tag.TargetSha, t => t);

            var versionTags = (branch.Commits?.SelectMany(c => tagsBySha[c.Sha].Select(t => t.Semver)) ?? Enumerable.Empty<SemanticVersion>()).ToList();

            this.semanticVersionTagsOnBranchCache.Add(branch, versionTags);
            return versionTags;
        }
    }

    public IEnumerable<(ITag Tag, SemanticVersion Semver, ICommit Commit)> GetValidVersionTags(string? tagPrefixRegex, DateTimeOffset? olderThan = null)
    {
        var tags = new List<(ITag, SemanticVersion, ICommit)>();

        foreach (var tag in this.repository.Tags)
        {
            if (!SemanticVersion.TryParse(tag.Name.Friendly, tagPrefixRegex, out var semver))
                continue;

            var commit = tag.PeeledTargetCommit();

            if (commit == null)
                continue;

            if (olderThan.HasValue && commit.When > olderThan.Value)
                continue;

            tags.Add((tag, semver, commit));
        }

        return tags;
    }

    public IEnumerable<ICommit> GetCommitLog(ICommit? baseVersionSource, ICommit? currentCommit)
    {
        var filter = new CommitFilter { IncludeReachableFrom = currentCommit, ExcludeReachableFrom = baseVersionSource, SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time };

        return this.repository.Commits.QueryBy(filter);
    }

    public bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit)
    {
        var filter = new CommitFilter { IncludeReachableFrom = branch, ExcludeReachableFrom = baseVersionSource, FirstParentOnly = true };
        var commitCollection = this.repository.Commits.QueryBy(filter);
        return commitCollection.Contains(firstMatchingCommit);
    }

    public ICommit? FindMergeBase(ICommit commit, ICommit mainlineTip) => this.repository.FindMergeBase(commit, mainlineTip);

    public int GetNumberOfUncommittedChanges() => this.repository.GetNumberOfUncommittedChanges();

    private static bool IsReleaseBranch(INamedReference branch, IEnumerable<KeyValuePair<string, BranchConfiguration>> releaseBranchConfig)
        => releaseBranchConfig.Any(c => c.Value?.Regex != null && Regex.IsMatch(branch.Name.Friendly, c.Value.Regex));

    private static IEnumerable<SemanticVersion> GetCurrentCommitSemanticVersions(ICommit? commit, string? tagPrefix, ITag tag)
    {
        var targetCommit = tag.PeeledTargetCommit();
        var tagName = tag.Name.Friendly;

        return targetCommit != null && Equals(targetCommit, commit) && SemanticVersion.TryParse(tagName, tagPrefix, out var version)
            ? new[] { version }
            : Array.Empty<SemanticVersion>();
    }
}
