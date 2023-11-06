using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion;

internal class RepositoryStore : IRepositoryStore
{
    private readonly ILog log;
    private readonly IGitRepository repository;

    private IReadOnlyList<SemanticVersionWithTag>? taggedSemanticVersionsCache;
    private readonly Dictionary<IBranch, IReadOnlyList<SemanticVersionWithTag>> taggedSemanticVersionsOnBranchCache = new();

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
        return currentBranch.Tip;
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
        desiredBranch = this.repository.Branches.Where(b => b.Name.EquivalentTo(targetBranchName)).MinBy(b => b.IsRemote);

        // Failsafe in case the specified branch is invalid
        desiredBranch ??= this.repository.Head;

        return desiredBranch;
    }

    public IBranch? FindBranch(string? branchName) => this.repository.Branches.FirstOrDefault(x => x.Name.EquivalentTo(branchName));

    public IBranch? FindMainBranch(IGitVersionConfiguration configuration)
    {
        var branches = configuration.Branches;
        var mainBranchRegex = branches[ConfigurationConstants.MainBranchKey].RegularExpression
            ?? branches[ConfigurationConstants.MasterBranchKey].RegularExpression;

        if (mainBranchRegex == null)
        {
            return FindBranch(ConfigurationConstants.MainBranchKey) ?? FindBranch(ConfigurationConstants.MasterBranchKey);
        }

        return this.repository.Branches.FirstOrDefault(b =>
            Regex.IsMatch(b.Name.WithoutOrigin, mainBranchRegex, RegexOptions.IgnoreCase));
    }

    public IEnumerable<IBranch> FindMainlineBranches(IGitVersionConfiguration configuration)
    {
        configuration.NotNull();

        foreach (var branch in this.repository.Branches)
        {
            var branchConfiguration = configuration.GetBranchConfiguration(branch.Name);
            if (branchConfiguration.IsMainline == true)
            {
                yield return branch;
            }
        }
    }

    public IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, IBranchConfiguration>> releaseBranchConfig)
        => this.repository.Branches.Where(b => IsReleaseBranch(b, releaseBranchConfig));

    private static bool IsReleaseBranch(INamedReference branch, IEnumerable<KeyValuePair<string, IBranchConfiguration>> releaseBranchConfig)
        => releaseBranchConfig.Any(c => c.Value.RegularExpression != null && Regex.IsMatch(branch.Name.WithoutOrigin, c.Value.RegularExpression));

    public IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude) => this.repository.Branches.ExcludeBranches(branchesToExclude);

    public IEnumerable<IBranch> GetBranchesContainingCommit(ICommit? commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false)
    {
        var branchesContainingCommitFinder = new BranchesContainingCommitFinder(this.repository, this.log);
        return branchesContainingCommitFinder.GetBranchesContainingCommit(commit, branches, onlyTrackedBranches);
    }

    public IDictionary<string, List<IBranch>> GetMainlineBranches(ICommit commit, IGitVersionConfiguration configuration)
    {
        var mainlineBranchFinder = new MainlineBranchFinder(this, this.repository, configuration, this.log);
        return mainlineBranchFinder.FindMainlineBranches(commit);
    }

    public IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration,
            params IBranch[] excludedBranches)
        => GetSourceBranches(branch, configuration, (IEnumerable<IBranch>)excludedBranches);

    public IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches)
    {
        var returnedBranches = new HashSet<IBranch>();

        var referenceLookup = this.repository.Refs.ToLookup(r => r.TargetIdentifier);

        foreach (var branchGrouping in FindCommitBranchesWasBranchedFrom(branch, configuration, excludedBranches)
            .GroupBy(element => element.Commit, element => element.Branch))
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
    public BranchCommit FindCommitBranchWasBranchedFrom(IBranch? branch, IGitVersionConfiguration configuration,
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

    public IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches)
        => FindCommitBranchesWasBranchedFrom(branch, configuration, (IEnumerable<IBranch>)excludedBranches);

    public IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches)
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

    public SemanticVersion? GetCurrentCommitTaggedVersion(ICommit? commit, string? tagPrefix, SemanticVersionFormat format, bool handleDetachedBranch)
        => this.repository.Tags
            .SelectMany(tag => GetCurrentCommitSemanticVersions(commit, tagPrefix, tag, format, handleDetachedBranch))
            .Max();

    public IEnumerable<SemanticVersion> GetVersionTagsOnBranch(IBranch branch, string? tagPrefix, SemanticVersionFormat format)
    {
        branch = branch.NotNull();

        if (this.taggedSemanticVersionsOnBranchCache.TryGetValue(branch, out var onBranch))
        {
            this.log.Debug($"Cache hit for version tags on branch '{branch.Name.Canonical}");
            return onBranch.Select(element => element.Value);
        }

        using (this.log.IndentLog($"Getting version tags from branch '{branch.Name.Canonical}'."))
        {
            var semanticVersions = GetTaggedSemanticVersions(tagPrefix, format);
            var tagsBySha = semanticVersions.Where(t => t.Tag.TargetSha != null).ToLookup(t => t.Tag.TargetSha, t => t);

            var versionTags = (branch.Commits?.SelectMany(c => tagsBySha[c.Sha].Select(t => t))
                ?? Enumerable.Empty<SemanticVersionWithTag>()).ToList();

            this.taggedSemanticVersionsOnBranchCache.Add(branch, versionTags);
            return versionTags.Select(element => element.Value);
        }
    }

    public IReadOnlyList<SemanticVersionWithTag> GetTaggedSemanticVersions(string? tagPrefix, SemanticVersionFormat format)
    {
        if (this.taggedSemanticVersionsCache != null)
        {
            this.log.Debug($"Returning cached tagged semantic versions. TagPrefix: {tagPrefix} and Format: {format}");
            return this.taggedSemanticVersionsCache;
        }

        this.log.Info($"Getting tagged semantic versions. TagPrefix: {tagPrefix} and Format: {format}");
        this.taggedSemanticVersionsCache = GetTaggedSemanticVersionsInternal().ToList();
        return this.taggedSemanticVersionsCache;

        IEnumerable<SemanticVersionWithTag> GetTaggedSemanticVersionsInternal()
        {
            foreach (var tag in this.repository.Tags)
            {
                if (SemanticVersion.TryParse(tag.Name.Friendly, tagPrefix, out var semanticVersion, format))
                {
                    yield return new SemanticVersionWithTag(semanticVersion, tag);
                }
            }
        }
    }

    public IReadOnlyList<SemanticVersionWithTag> GetTaggedSemanticVersionsOnBranch(
        IBranch branch, string? tagPrefix, SemanticVersionFormat format)
    {
        branch = branch.NotNull();

        if (this.taggedSemanticVersionsOnBranchCache.TryGetValue(branch, out var onBranch))
        {
            this.log.Debug($"Returning cached tagged semantic versions from '{branch.Name.Canonical}'. TagPrefix: {tagPrefix} and Format: {format}");
            return onBranch;
        }

        using (this.log.IndentLog($"Getting tagged semantic versions from '{branch.Name.Canonical}'.  TagPrefix: {tagPrefix} and Format: {format}"))
        {
            var semanticVersions = GetTaggedSemanticVersions(tagPrefix, format);
            var tagsBySha = semanticVersions.Where(t => t.Tag.TargetSha != null).ToLookup(t => t.Tag.TargetSha, t => t);

            var versionTags = (branch.Commits?.SelectMany(c => tagsBySha[c.Sha].Select(t => t))
                ?? Enumerable.Empty<SemanticVersionWithTag>()).ToList();

            this.taggedSemanticVersionsOnBranchCache.Add(branch, versionTags);
            return versionTags;
        }
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

    private IEnumerable<SemanticVersion> GetCurrentCommitSemanticVersions(ICommit? commit, string? tagPrefix, ITag tag, SemanticVersionFormat versionFormat, bool handleDetachedBranch)
    {
        if (commit == null)
            return Array.Empty<SemanticVersion>();

        var commitToCompare = handleDetachedBranch ? FindMergeBase(commit, tag.Commit) : commit;

        var tagName = tag.Name.Friendly;

        return Equals(tag.Commit, commitToCompare) && SemanticVersion.TryParse(tagName, tagPrefix, out var version, versionFormat)
            ? new[] { version }
            : Array.Empty<SemanticVersion>();
    }
}
