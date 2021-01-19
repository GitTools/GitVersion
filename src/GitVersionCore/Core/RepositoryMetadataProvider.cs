using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion
{
    public class RepositoryMetadataProvider : IRepositoryMetadataProvider
    {
        private readonly Dictionary<IBranch, List<BranchCommit>> mergeBaseCommitsCache = new();
        private readonly Dictionary<Tuple<IBranch, IBranch>, MergeBaseData> mergeBaseCache = new();
        private readonly Dictionary<IBranch, List<SemanticVersion>> semanticVersionTagsOnBranchCache = new();
        private const string MissingTipFormat = "{0} has no tip. Please see http://example.com/docs for information on how to fix this.";

        private readonly ILog log;
        private readonly IGitRepository repository;

        public RepositoryMetadataProvider(ILog log, IGitRepository repository)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.repository = repository ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
        /// </summary>
        public ICommit FindMergeBase(IBranch branch, IBranch otherBranch)
        {
            var key = Tuple.Create(branch, otherBranch);

            if (mergeBaseCache.ContainsKey(key))
            {
                log.Debug($"Cache hit for merge base between '{branch}' and '{otherBranch}'.");
                return mergeBaseCache[key].MergeBase;
            }

            using (log.IndentLog($"Finding merge base between '{branch}' and '{otherBranch}'."))
            {
                // Other branch tip is a forward merge
                var commitToFindCommonBase = otherBranch.Tip;
                var commit = branch.Tip;
                if (otherBranch.Tip.Parents.Contains(commit))
                {
                    commitToFindCommonBase = otherBranch.Tip.Parents.First();
                }

                var findMergeBase = repository.FindMergeBase(commit, commitToFindCommonBase);
                if (findMergeBase != null)
                {
                    log.Info($"Found merge base of {findMergeBase}");
                    // We do not want to include merge base commits which got forward merged into the other branch
                    ICommit forwardMerge;
                    do
                    {
                        // Now make sure that the merge base is not a forward merge
                        forwardMerge = repository.GetForwardMerge(commitToFindCommonBase, findMergeBase);

                        if (forwardMerge != null)
                        {
                            // TODO Fix the logging up in this section
                            var second = forwardMerge.Parents.First();
                            log.Debug($"Second {second}");
                            var mergeBase = repository.FindMergeBase(commit, second);
                            if (mergeBase == null)
                            {
                                log.Warning("Could not find mergbase for " + commit);
                            }
                            else
                            {
                                log.Debug($"New Merge base {mergeBase}");
                            }
                            if (Equals(mergeBase, findMergeBase))
                            {
                                log.Debug("Breaking");
                                break;
                            }
                            findMergeBase = mergeBase;
                            commitToFindCommonBase = second;
                            log.Info($"Merge base was due to a forward merge, next merge base is {findMergeBase}");
                        }
                    } while (forwardMerge != null);
                }

                // Store in cache.
                mergeBaseCache.Add(key, new MergeBaseData(findMergeBase));

                log.Info($"Merge base of {branch}' and '{otherBranch} is {findMergeBase}");
                return findMergeBase;
            }
        }

        public ICommit FindMergeBase(ICommit commit, ICommit mainlineTip)
        {
            return repository.FindMergeBase(commit, mainlineTip);
        }

        public ICommit GetCurrentCommit(IBranch currentBranch, string commitId)
        {
            ICommit currentCommit = null;
            if (!string.IsNullOrWhiteSpace(commitId))
            {
                log.Info($"Searching for specific commit '{commitId}'");

                var commit = repository.Commits.FirstOrDefault(c => string.Equals(c.Sha, commitId, StringComparison.OrdinalIgnoreCase));
                if (commit != null)
                {
                    currentCommit = commit;
                }
                else
                {
                    log.Warning($"Commit '{commitId}' specified but not found");
                }
            }

            if (currentCommit == null)
            {
                log.Info("Using latest commit on specified branch");
                currentCommit = currentBranch.Tip;
            }

            return currentCommit;
        }
        public ICommit GetBaseVersionSource(ICommit currentBranchTip)
        {
            return repository.GetBaseVersionSource(currentBranchTip);
        }
        public IEnumerable<ICommit> GetMainlineCommitLog(ICommit baseVersionSource, ICommit mainlineTip)
        {
            return repository.GetMainlineCommitLog(baseVersionSource, mainlineTip);
        }
        public IEnumerable<ICommit> GetMergeBaseCommits(ICommit mergeCommit, ICommit mergedHead, ICommit findMergeBase)
        {
            return repository.GetMergeBaseCommits(mergeCommit, mergedHead, findMergeBase);
        }

        public IBranch GetTargetBranch(string targetBranchName)
        {
            // By default, we assume HEAD is pointing to the desired branch
            var desiredBranch = repository.Head;

            // Make sure the desired branch has been specified
            if (!string.IsNullOrEmpty(targetBranchName))
            {
                // There are some edge cases where HEAD is not pointing to the desired branch.
                // Therefore it's important to verify if 'currentBranch' is indeed the desired branch.

                var targetBranch = FindBranch(targetBranchName);
                // CanonicalName can be "refs/heads/develop", so we need to check for "/{TargetBranch}" as well
                if (!desiredBranch.Equals(targetBranch))
                {
                    // In the case where HEAD is not the desired branch, try to find the branch with matching name
                    desiredBranch = repository.Branches?
                        .Where(b => b.Name.EquivalentTo(targetBranchName))
                        .OrderBy(b => b.IsRemote)
                        .FirstOrDefault();

                    // Failsafe in case the specified branch is invalid
                    desiredBranch ??= repository.Head;
                }
            }

            return desiredBranch;
        }

        public IBranch FindBranch(string branchName) => repository.Branches.FirstOrDefault(x => x.Name.EquivalentTo(branchName));

        public IBranch GetChosenBranch(Config configuration)
        {
            var developBranchRegex = configuration.Branches[Config.DevelopBranchKey].Regex;
            var masterBranchRegex = configuration.Branches[Config.MasterBranchKey].Regex;

            var chosenBranch = repository.Branches.FirstOrDefault(b =>
                Regex.IsMatch(b.Name.Friendly, developBranchRegex, RegexOptions.IgnoreCase) ||
                Regex.IsMatch(b.Name.Friendly, masterBranchRegex, RegexOptions.IgnoreCase));

            return chosenBranch;
        }

        public IEnumerable<IBranch> GetBranchesForCommit(ICommit commit)
        {
            return repository.Branches.Where(b => !b.IsRemote && Equals(b.Tip, commit)).ToList();
        }

        public IEnumerable<IBranch> GetExcludedInheritBranches(Config configuration)
        {
            return repository.Branches.Where(b =>
            {
                var branchConfig = configuration.GetConfigForBranch(b.Name.WithoutRemote);

                return branchConfig == null || branchConfig.Increment == IncrementStrategy.Inherit;
            }).ToList();
        }

        public IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, BranchConfig>> releaseBranchConfig)
        {
            return repository.Branches
                .Where(b => releaseBranchConfig.Any(c => Regex.IsMatch(b.Name.Friendly, c.Value.Regex)));
        }

        public IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude)
        {
            return repository.Branches.ExcludeBranches(branchesToExclude);
        }

        // TODO Should we cache this?
        public IEnumerable<IBranch> GetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch> branches = null, bool onlyTrackedBranches = false)
        {
            branches ??= repository.Branches.ToList();
            static bool IncludeTrackedBranches(IBranch branch, bool includeOnlyTracked) => includeOnlyTracked && branch.IsTracking || !includeOnlyTracked;

            if (commit == null)
            {
                throw new ArgumentNullException(nameof(commit));
            }

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

                    var commits = repository.GetCommitsReacheableFrom(commit, branch);

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

        public Dictionary<string, List<IBranch>> GetMainlineBranches(ICommit commit, IEnumerable<KeyValuePair<string, BranchConfig>> mainlineBranchConfigs)
        {
            return repository.Branches
                .Where(b =>
                {
                    return mainlineBranchConfigs.Any(c => Regex.IsMatch(b.Name.Friendly, c.Value.Regex));
                })
                .Select(b => new
                {
                    MergeBase = FindMergeBase(b.Tip, commit),
                    Branch = b
                })
                .Where(a => a.MergeBase != null)
                .GroupBy(b => b.MergeBase.Sha, b => b.Branch)
                .ToDictionary(b => b.Key, b => b.ToList());
        }

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

            using (log.IndentLog($"Finding branch source of '{branch}'"))
            {
                if (branch.Tip == null)
                {
                    log.Warning(string.Format(MissingTipFormat, branch));
                    return BranchCommit.Empty;
                }

                var possibleBranches = GetMergeCommitsForBranch(branch, configuration, excludedBranches)
                    .Where(b => !branch.Equals(b.Branch))
                    .ToList();

                if (possibleBranches.Count > 1)
                {
                    var first = possibleBranches.First();
                    log.Info($"Multiple source branches have been found, picking the first one ({first.Branch}).{System.Environment.NewLine}" +
                             $"This may result in incorrect commit counting.{System.Environment.NewLine}Options were:{System.Environment.NewLine}" +
                             string.Join(", ", possibleBranches.Select(b => b.Branch.ToString())));
                    return first;
                }

                return possibleBranches.SingleOrDefault();
            }
        }

        public SemanticVersion GetCurrentCommitTaggedVersion(ICommit commit, EffectiveConfiguration config)
        {
            return repository.Tags
                .SelectMany(t =>
                {
                    var targetCommit = t.PeeledTargetCommit();
                    if (targetCommit != null && Equals(targetCommit, commit) && SemanticVersion.TryParse(t.Name.Friendly, config.GitTagPrefix, out var version))
                        return new[]
                        {
                            version
                        };
                    return new SemanticVersion[0];
                })
                .Max();
        }

        public SemanticVersion MaybeIncrement(BaseVersion baseVersion, GitVersionContext context)
        {
            var increment = IncrementStrategyFinder.DetermineIncrementedField(repository, context, baseVersion);
            return increment != null ? baseVersion.SemanticVersion.IncrementVersion(increment.Value) : baseVersion.SemanticVersion;
        }

        public IEnumerable<SemanticVersion> GetVersionTagsOnBranch(IBranch branch, string tagPrefixRegex)
        {
            if (semanticVersionTagsOnBranchCache.ContainsKey(branch))
            {
                log.Debug($"Cache hit for version tags on branch '{branch.Name.Canonical}");
                return semanticVersionTagsOnBranchCache[branch];
            }

            using (log.IndentLog($"Getting version tags from branch '{branch.Name.Canonical}'."))
            {
                var tags = GetValidVersionTags(tagPrefixRegex);

                var versionTags = branch.Commits.SelectMany(c => tags.Where(t => c.Sha == t.Item1.TargetSha).Select(t => t.Item2)).ToList();

                semanticVersionTagsOnBranchCache.Add(branch, versionTags);
                return versionTags;
            }
        }

        public IEnumerable<Tuple<ITag, SemanticVersion>> GetValidVersionTags(string tagPrefixRegex, DateTimeOffset? olderThan = null)
        {
            var tags = new List<Tuple<ITag, SemanticVersion>>();

            foreach (var tag in repository.Tags)
            {
                var commit = tag.PeeledTargetCommit();

                if (commit == null)
                    continue;

                if (olderThan.HasValue && commit.When > olderThan.Value)
                    continue;

                if (SemanticVersion.TryParse(tag.Name.Friendly, tagPrefixRegex, out var semver))
                {
                    tags.Add(Tuple.Create(tag, semver));
                }
            }

            return tags;
        }

        public IEnumerable<ICommit> GetCommitLog(ICommit baseVersionSource, ICommit currentCommit)
        {
            return repository.GetCommitLog(baseVersionSource, currentCommit);
        }

        public string ShortenObjectId(ICommit commit)
        {
            return repository.ShortenObjectId(commit);
        }

        public VersionField? DetermineIncrementedField(BaseVersion baseVersion, GitVersionContext context)
        {
            return IncrementStrategyFinder.DetermineIncrementedField(repository, context, baseVersion);
        }

        public bool GetMatchingCommitBranch(ICommit baseVersionSource, IBranch branch, ICommit firstMatchingCommit)
        {
            return repository.GetMatchingCommitBranch(baseVersionSource, branch, firstMatchingCommit);
        }

        private IEnumerable<BranchCommit> GetMergeCommitsForBranch(IBranch branch, Config configuration, IEnumerable<IBranch> excludedBranches)
        {
            if (mergeBaseCommitsCache.ContainsKey(branch))
            {
                log.Debug($"Cache hit for getting merge commits for branch {branch.Name.Canonical}.");
                return mergeBaseCommitsCache[branch];
            }

            var currentBranchConfig = configuration.GetConfigForBranch(branch.Name.WithoutRemote);
            var regexesToCheck = currentBranchConfig == null
                ? new[] { ".*" } // Match anything if we can't find a branch config
                : currentBranchConfig.SourceBranches.Select(sb => configuration.Branches[sb].Regex);
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
                        log.Warning(string.Format(MissingTipFormat, otherBranch));
                        return BranchCommit.Empty;
                    }

                    var findMergeBase = FindMergeBase(branch, otherBranch);
                    return new BranchCommit(findMergeBase, otherBranch);
                })
                .Where(b => b.Commit != null)
                .OrderByDescending(b => b.Commit.When)
                .ToList();
            mergeBaseCommitsCache.Add(branch, branchMergeBases);

            return branchMergeBases;
        }

        public int GetNumberOfUncommittedChanges() => repository.GetNumberOfUncommittedChanges();

        private class MergeBaseData
        {
            public ICommit MergeBase { get; }

            public MergeBaseData(ICommit mergeBase)
            {
                MergeBase = mergeBase;
            }
        }
    }

}
