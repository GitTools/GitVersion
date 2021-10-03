using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion;

public class RepositoryStore : IRepositoryStore
{
    private readonly Dictionary<IBranch, List<BranchCommit>> mergeBaseCommitsCache = new();
    private readonly Dictionary<Tuple<IBranch, IBranch?>, ICommit?> mergeBaseCache = new();
    private readonly Dictionary<IBranch, List<SemanticVersion>> semanticVersionTagsOnBranchCache = new();
    private const string MissingTipFormat = "{0} has no tip. Please see http://example.com/docs for information on how to fix this.";

    private readonly ILog log;
    private readonly IGitRepository repository;
    private readonly IIncrementStrategyFinder incrementStrategyFinder;

    public RepositoryStore(ILog log, IGitRepository repository, IIncrementStrategyFinder incrementStrategyFinder)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.repository = repository ?? throw new ArgumentNullException(nameof(log));
        this.incrementStrategyFinder = incrementStrategyFinder ?? throw new ArgumentNullException(nameof(incrementStrategyFinder));
    }

    /// <summary>
    /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
    /// </summary>
    public ICommit? FindMergeBase(IBranch branch, IBranch? otherBranch)
    {
        var key = Tuple.Create(branch, otherBranch);

        if (this.mergeBaseCache.ContainsKey(key))
        {
            this.log.Debug($"Cache hit for merge base between '{branch}' and '{otherBranch}'.");
            return this.mergeBaseCache[key];
        }

        using (this.log.IndentLog($"Finding merge base between '{branch}' and '{otherBranch}'."))
        {
            // Other branch tip is a forward merge
            var commitToFindCommonBase = otherBranch?.Tip;
            var commit = branch.Tip!;
            if (commitToFindCommonBase?.Parents.Contains(commit) == true)
            {
                commitToFindCommonBase = otherBranch!.Tip!.Parents.First();
            }

            var findMergeBase = FindMergeBase(commit, commitToFindCommonBase!);
            if (findMergeBase != null)
            {
                this.log.Info($"Found merge base of {findMergeBase}");
                // We do not want to include merge base commits which got forward merged into the other branch
                ICommit forwardMerge;
                do
                {
                    // Now make sure that the merge base is not a forward merge
                    forwardMerge = GetForwardMerge(commitToFindCommonBase, findMergeBase);

                    if (forwardMerge != null)
                    {
                        // TODO Fix the logging up in this section
                        var second = forwardMerge.Parents.First();
                        this.log.Debug($"Second {second}");
                        var mergeBase = FindMergeBase(commit, second);
                        if (mergeBase == null)
                        {
                            this.log.Warning("Could not find mergbase for " + commit);
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
                    }
                } while (forwardMerge != null);
            }

            // Store in cache.
            this.mergeBaseCache.Add(key, findMergeBase);

            this.log.Info($"Merge base of {branch}' and '{otherBranch} is {findMergeBase}");
            return findMergeBase;
        }
    }

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

        if (currentCommit == null)
        {
            this.log.Info("Using latest commit on specified branch");
            currentCommit = currentBranch.Tip;
        }

        return currentCommit;
    }
    public ICommit GetBaseVersionSource(ICommit currentBranchTip)
    {
        try
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = currentBranchTip
            };
            var commitCollection = this.repository.Commits.QueryBy(filter);

            return commitCollection.First(c => !c.Parents.Any());
        }
        catch (Exception exception)
        {
            throw new GitVersionException($"Cannot find commit {currentBranchTip}. Please ensure that the repository is an unshallow clone with `git fetch --unshallow`.", exception);
        }
    }
    public IEnumerable<ICommit> GetMainlineCommitLog(ICommit? baseVersionSource, ICommit? mainlineTip)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = mainlineTip,
            ExcludeReachableFrom = baseVersionSource,
            SortBy = CommitSortStrategies.Reverse,
            FirstParentOnly = true
        };

        return this.repository.Commits.QueryBy(filter);
    }
    public IEnumerable<ICommit> GetMergeBaseCommits(ICommit? mergeCommit, ICommit? mergedHead, ICommit? findMergeBase)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = mergedHead,
            ExcludeReachableFrom = findMergeBase
        };
        var commitCollection = this.repository.Commits.QueryBy(filter);

        var commits = mergeCommit != null
            ? new[]
            {
                mergeCommit
            }.Union(commitCollection)
            : commitCollection;
        return commits;
    }

    public IBranch GetTargetBranch(string? targetBranchName)
    {
        // By default, we assume HEAD is pointing to the desired branch
        var desiredBranch = this.repository.Head;

        // Make sure the desired branch has been specified
        if (!targetBranchName.IsNullOrEmpty())
        {
            // There are some edge cases where HEAD is not pointing to the desired branch.
            // Therefore it's important to verify if 'currentBranch' is indeed the desired branch.

            var targetBranch = FindBranch(targetBranchName);
            // CanonicalName can be "refs/heads/develop", so we need to check for "/{TargetBranch}" as well
            if (!desiredBranch.Equals(targetBranch))
            {
                // In the case where HEAD is not the desired branch, try to find the branch with matching name
                desiredBranch = this.repository.Branches?
                    .Where(b => b.Name.EquivalentTo(targetBranchName))
                    .OrderBy(b => b.IsRemote)
                    .FirstOrDefault();

                // Failsafe in case the specified branch is invalid
                desiredBranch ??= this.repository.Head;
            }
        }

        return desiredBranch;
    }

    public IBranch FindBranch(string? branchName) => this.repository.Branches.FirstOrDefault(x => x.Name.EquivalentTo(branchName));

    public IBranch GetChosenBranch(Config configuration)
    {
        var developBranchRegex = configuration.Branches[Config.DevelopBranchKey]?.Regex;
        var mainBranchRegex = configuration.Branches[Config.MainBranchKey]?.Regex;

        var chosenBranch = this.repository.Branches.FirstOrDefault(b =>
            Regex.IsMatch(b.Name.Friendly, developBranchRegex, RegexOptions.IgnoreCase) ||
            Regex.IsMatch(b.Name.Friendly, mainBranchRegex, RegexOptions.IgnoreCase));

        return chosenBranch;
    }

    public IEnumerable<IBranch> GetBranchesForCommit(ICommit commit) => this.repository.Branches.Where(b => !b.IsRemote && Equals(b.Tip, commit)).ToList();

    public IEnumerable<IBranch> GetExcludedInheritBranches(Config configuration) => this.repository.Branches.Where(b =>
    {
        var branchConfig = configuration.GetConfigForBranch(b.Name.WithoutRemote);

        return branchConfig == null || branchConfig.Increment == IncrementStrategy.Inherit;
    }).ToList();

    public IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, BranchConfig?>>? releaseBranchConfig) =>
        this.repository.Branches
            .Where(b => releaseBranchConfig?.Any(c => Regex.IsMatch(b.Name.Friendly, c.Value?.Regex)) == true);

    public IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude) => this.repository.Branches.ExcludeBranches(branchesToExclude);

    // TODO Should we cache this?
    public IEnumerable<IBranch> GetBranchesContainingCommit(ICommit? commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false)
    {
        if (commit == null)
        {
            throw new ArgumentNullException(nameof(commit));
        }

        return InnerGetBranchesContainingCommit(commit, branches, onlyTrackedBranches, this.repository, this.log);

        static bool IncludeTrackedBranches(IBranch branch, bool includeOnlyTracked) => includeOnlyTracked && branch.IsTracking || !includeOnlyTracked;

        // Yielding part is split from the main part of the method to avoid having the exception check performed lazily.
        // Details at https://github.com/GitTools/GitVersion/issues/2755
        static IEnumerable<IBranch> InnerGetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch>? branches, bool onlyTrackedBranches, IGitRepository repository, ILog log)
        {
            branches ??= repository.Branches.ToList();

            using (log.IndentLog($"Getting branches containing the commit '{commit.Id}'."))
            {
                var directBranchHasBeenFound = false;
                log.Info("Trying to find direct branches.");
                // TODO: It looks wasteful looping through the branches twice. Can't these loops be merged somehow? @asbjornu
                var branchList = branches.ToList();
                foreach (var branch in branchList)
                {
                    if (branch.Tip != null && branch.Tip.Sha != commit.Sha || IncludeTrackedBranches(branch, onlyTrackedBranches))
                    {
                        continue;
                    }

                    directBranchHasBeenFound = true;
                    log.Info($"Direct branch found: '{branch}'.");
                    yield return branch;
                }

                if (directBranchHasBeenFound)
                {
                    yield break;
                }

                log.Info($"No direct branches found, searching through {(onlyTrackedBranches ? "tracked" : "all")} branches.");
                foreach (var branch in branchList.Where(b => IncludeTrackedBranches(b, onlyTrackedBranches)))
                {
                    log.Info($"Searching for commits reachable from '{branch}'.");

                    var commits = GetCommitsReacheableFrom(repository, commit, branch);

                    if (!commits.Any())
                    {
                        log.Info($"The branch '{branch}' has no matching commits.");
                        continue;
                    }

                    log.Info($"The branch '{branch}' has a matching commit.");
                    yield return branch;
                }
            }
        }
    }

    public Dictionary<string, List<IBranch>> GetMainlineBranches(ICommit commit, IEnumerable<KeyValuePair<string, BranchConfig?>>? mainlineBranchConfigs) =>
        this.repository.Branches
            .Where(b => mainlineBranchConfigs?.Any(c => Regex.IsMatch(b.Name.Friendly, c.Value?.Regex)) == true)
            .Select(b => new
            {
                MergeBase = FindMergeBase(b.Tip!, commit),
                Branch = b
            })
            .Where(a => a.MergeBase != null)
            .GroupBy(b => b.MergeBase.Sha, b => b.Branch)
            .ToDictionary(b => b.Key, b => b.ToList());

    /// <summary>
    /// Find the commit where the given branch was branched from another branch.
    /// If there are multiple such commits and branches, tries to guess based on commit histories.
    /// </summary>
    public BranchCommit FindCommitBranchWasBranchedFrom(IBranch branch, Config configuration, params IBranch[] excludedBranches)
    {
        if (branch == null)
        {
            throw new ArgumentNullException(nameof(branch));
        }

        using (this.log.IndentLog($"Finding branch source of '{branch}'"))
        {
            if (branch.Tip == null)
            {
                this.log.Warning(string.Format(MissingTipFormat, branch));
                return BranchCommit.Empty;
            }

            var possibleBranches = GetMergeCommitsForBranch(branch, configuration, excludedBranches)
                .Where(b => !branch.Name.EquivalentTo(b.Branch.Name.WithoutRemote))
                .ToList();

            if (possibleBranches.Count > 1)
            {
                var first = possibleBranches.First();
                this.log.Info($"Multiple source branches have been found, picking the first one ({first.Branch}).{System.Environment.NewLine}" +
                              $"This may result in incorrect commit counting.{System.Environment.NewLine}Options were:{System.Environment.NewLine}" +
                              string.Join(", ", possibleBranches.Select(b => b.Branch.ToString())));
                return first;
            }

            return possibleBranches.SingleOrDefault();
        }
    }

    public SemanticVersion GetCurrentCommitTaggedVersion(ICommit? commit, EffectiveConfiguration config) =>
        this.repository.Tags
            .SelectMany(t =>
            {
                var targetCommit = t.PeeledTargetCommit();
                if (targetCommit != null && Equals(targetCommit, commit) && SemanticVersion.TryParse(t.Name.Friendly, config.GitTagPrefix, out var version))
                    return new[]
                    {
                        version
                    };
                return Array.Empty<SemanticVersion>();
            })
            .Max();

    public SemanticVersion MaybeIncrement(BaseVersion baseVersion, GitVersionContext context)
    {
        var increment = this.incrementStrategyFinder.DetermineIncrementedField(this.repository, context, baseVersion);
        return increment != null ? baseVersion.SemanticVersion.IncrementVersion(increment.Value) : baseVersion.SemanticVersion;
    }

    public IEnumerable<SemanticVersion> GetVersionTagsOnBranch(IBranch branch, string? tagPrefixRegex)
    {
        if (this.semanticVersionTagsOnBranchCache.ContainsKey(branch))
        {
            this.log.Debug($"Cache hit for version tags on branch '{branch.Name.Canonical}");
            return this.semanticVersionTagsOnBranchCache[branch];
        }

        using (this.log.IndentLog($"Getting version tags from branch '{branch.Name.Canonical}'."))
        {
            var tags = GetValidVersionTags(tagPrefixRegex);
            var tagsBySha = tags.Where(t => t.Item1.TargetSha != null).ToLookup(t => t.Item1.TargetSha, t => t);
            var versionTags = branch.Commits.SelectMany(c => tagsBySha[c.Sha].Select(t => t.Item2)).ToList();

            this.semanticVersionTagsOnBranchCache.Add(branch, versionTags);
            return versionTags;
        }
    }

    public IEnumerable<Tuple<ITag, SemanticVersion, ICommit>> GetValidVersionTags(string? tagPrefixRegex, DateTimeOffset? olderThan = null)
    {
        var tags = new List<Tuple<ITag, SemanticVersion, ICommit>>();

        foreach (var tag in this.repository.Tags)
        {
            if (!SemanticVersion.TryParse(tag.Name.Friendly, tagPrefixRegex, out var semver))
                continue;

            var commit = tag.PeeledTargetCommit();

            if (commit == null)
                continue;

            if (olderThan.HasValue && commit.When > olderThan.Value)
                continue;

            tags.Add(Tuple.Create(tag, semver, commit));
        }

        return tags;
    }

    public IEnumerable<ICommit> GetCommitLog(ICommit? baseVersionSource, ICommit? currentCommit)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = currentCommit,
            ExcludeReachableFrom = baseVersionSource,
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
        };

        return this.repository.Commits.QueryBy(filter);
    }

    public VersionField? DetermineIncrementedField(BaseVersion baseVersion, GitVersionContext context) =>
        this.incrementStrategyFinder.DetermineIncrementedField(this.repository, context, baseVersion);

    public bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = branch,
            ExcludeReachableFrom = baseVersionSource,
            FirstParentOnly = true,
        };
        var commitCollection = this.repository.Commits.QueryBy(filter);
        return commitCollection.Contains(firstMatchingCommit);
    }

    private IEnumerable<BranchCommit> GetMergeCommitsForBranch(IBranch branch, Config configuration, IEnumerable<IBranch> excludedBranches)
    {
        if (this.mergeBaseCommitsCache.ContainsKey(branch))
        {
            this.log.Debug($"Cache hit for getting merge commits for branch {branch.Name.Canonical}.");
            return this.mergeBaseCommitsCache[branch];
        }

        var currentBranchConfig = configuration.GetConfigForBranch(branch.Name.WithoutRemote);
        var regexesToCheck = currentBranchConfig == null
            ? new[] { ".*" } // Match anything if we can't find a branch config
            : currentBranchConfig.SourceBranches.Select(sb => configuration.Branches[sb]?.Regex);
        var branchMergeBases = ExcludingBranches(excludedBranches)
            .Where(b =>
            {
                if (Equals(b, branch)) return false;
                var branchCanBeMergeBase = regexesToCheck.Any(regex => Regex.IsMatch(b.Name.Friendly, regex));

                return branchCanBeMergeBase;
            })
            .Select(otherBranch =>
            {
                if (otherBranch.Tip == null)
                {
                    this.log.Warning(string.Format(MissingTipFormat, otherBranch));
                    return BranchCommit.Empty;
                }

                var findMergeBase = FindMergeBase(branch, otherBranch);
                return new BranchCommit(findMergeBase!, otherBranch);
            })
            .Where(b => b.Commit != null)
            .OrderByDescending(b => b.Commit.When)
            .ToList();
        this.mergeBaseCommitsCache.Add(branch, branchMergeBases);

        return branchMergeBases;
    }

    private static IEnumerable<ICommit> GetCommitsReacheableFrom(IGitRepository repository, ICommit commit, IBranch branch)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = branch
        };
        var commitCollection = repository.Commits.QueryBy(filter);

        return commitCollection.Where(c => c.Sha == commit.Sha);
    }

    private ICommit GetForwardMerge(ICommit? commitToFindCommonBase, ICommit? findMergeBase)
    {
        var filter = new CommitFilter
        {
            IncludeReachableFrom = commitToFindCommonBase,
            ExcludeReachableFrom = findMergeBase
        };
        var commitCollection = this.repository.Commits.QueryBy(filter);

        return commitCollection.FirstOrDefault(c => c.Parents.Contains(findMergeBase));
    }

    public ICommit FindMergeBase(ICommit commit, ICommit mainlineTip) => this.repository.FindMergeBase(commit, mainlineTip);

    public int GetNumberOfUncommittedChanges() => this.repository.GetNumberOfUncommittedChanges();
}
