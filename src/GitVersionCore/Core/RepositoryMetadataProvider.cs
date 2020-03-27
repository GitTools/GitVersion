using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion
{
    public class RepositoryMetadataProvider : IRepositoryMetadataProvider
    {
        private readonly Dictionary<Branch, List<BranchCommit>> mergeBaseCommitsCache = new Dictionary<Branch, List<BranchCommit>>();
        private readonly Dictionary<Tuple<Branch, Branch>, MergeBaseData> mergeBaseCache = new Dictionary<Tuple<Branch, Branch>, MergeBaseData>();
        private readonly Dictionary<Branch, List<SemanticVersion>> semanticVersionTagsOnBranchCache = new Dictionary<Branch, List<SemanticVersion>>();
        private const string MissingTipFormat = "{0} has no tip. Please see http://example.com/docs for information on how to fix this.";

        private readonly ILog log;
        private readonly IRepository repository;

        public RepositoryMetadataProvider(ILog log, IRepository repository)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.repository = repository ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
        /// </summary>
        public Commit FindMergeBase(Branch branch, Branch otherBranch)
        {
            var key = Tuple.Create(branch, otherBranch);

            if (mergeBaseCache.ContainsKey(key))
            {
                log.Debug($"Cache hit for merge base between '{branch.FriendlyName}' and '{otherBranch.FriendlyName}'.");
                return mergeBaseCache[key].MergeBase;
            }

            using (log.IndentLog($"Finding merge base between '{branch.FriendlyName}' and '{otherBranch.FriendlyName}'."))
            {
                // Otherbranch tip is a forward merge
                var commitToFindCommonBase = otherBranch.Tip;
                var commit = branch.Tip;
                if (otherBranch.Tip.Parents.Contains(commit))
                {
                    commitToFindCommonBase = otherBranch.Tip.Parents.First();
                }

                var findMergeBase = repository.ObjectDatabase.FindMergeBase(commit, commitToFindCommonBase);
                if (findMergeBase != null)
                {
                    log.Info($"Found merge base of {findMergeBase.Sha}");
                    // We do not want to include merge base commits which got forward merged into the other branch
                    Commit forwardMerge;
                    do
                    {
                        // Now make sure that the merge base is not a forward merge
                        forwardMerge = GetForwardMerge(commitToFindCommonBase, findMergeBase);

                        if (forwardMerge != null)
                        {
                            // TODO Fix the logging up in this section
                            var second = forwardMerge.Parents.First();
                            log.Debug("Second " + second.Sha);
                            var mergeBase = repository.ObjectDatabase.FindMergeBase(commit, second);
                            if (mergeBase == null)
                            {
                                log.Warning("Could not find mergbase for " + commit);
                            }
                            else
                            {
                                log.Debug("New Merge base " + mergeBase.Sha);
                            }
                            if (mergeBase == findMergeBase)
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

                log.Info($"Merge base of {branch.FriendlyName}' and '{otherBranch.FriendlyName} is {findMergeBase}");
                return findMergeBase;
            }
        }

        public Commit FindMergeBase(Commit commit, Commit mainlineTip)
        {
            return repository.ObjectDatabase.FindMergeBase(commit, mainlineTip);
        }

        public Commit GetCurrentCommit(Branch currentBranch, string commitId)
        {
            Commit currentCommit = null;
            if (!String.IsNullOrWhiteSpace(commitId))
            {
                log.Info($"Searching for specific commit '{commitId}'");

                var commit = repository.Commits.FirstOrDefault(c => String.Equals(c.Sha, commitId, StringComparison.OrdinalIgnoreCase));
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

        public Commit GetBaseVersionSource(Commit currentBranchTip)
        {
            var baseVersionSource = repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = currentBranchTip
            }).First(c => !c.Parents.Any());
            return baseVersionSource;
        }

        public List<Commit> GetMainlineCommitLog(Commit baseVersionSource, Commit mainlineTip)
        {
            var mainlineCommitLog = repository.Commits
                .QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = mainlineTip,
                    ExcludeReachableFrom = baseVersionSource,
                    SortBy = CommitSortStrategies.Reverse,
                    FirstParentOnly = true
                })
                .ToList();
            return mainlineCommitLog;
        }

        public IEnumerable<Commit> GetMergeBaseCommits(Commit mergeCommit, Commit mergedHead, Commit findMergeBase)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = mergedHead,
                ExcludeReachableFrom = findMergeBase
            };
            var query = repository.Commits.QueryBy(filter);

            var commits = mergeCommit == null ? query.ToList() : new[] {
                mergeCommit
            }.Union(query).ToList();
            return commits;
        }


        public Branch GetTargetBranch(string targetBranch)
        {
            // By default, we assume HEAD is pointing to the desired branch
            var desiredBranch = repository.Head;

            // Make sure the desired branch has been specified
            if (!String.IsNullOrEmpty(targetBranch))
            {
                // There are some edge cases where HEAD is not pointing to the desired branch.
                // Therefore it's important to verify if 'currentBranch' is indeed the desired branch.

                // CanonicalName can be "refs/heads/develop", so we need to check for "/{TargetBranch}" as well
                if (!desiredBranch.CanonicalName.IsBranch(targetBranch))
                {
                    // In the case where HEAD is not the desired branch, try to find the branch with matching name
                    desiredBranch = repository.Branches?
                        .SingleOrDefault(b =>
                            b.CanonicalName == targetBranch ||
                            b.FriendlyName == targetBranch ||
                            b.NameWithoutRemote() == targetBranch);

                    // Failsafe in case the specified branch is invalid
                    desiredBranch ??= repository.Head;
                }
            }

            return desiredBranch;
        }

        public Branch FindBranch(string branchName)
        {
            return repository.FindBranch(branchName);
        }

        public Branch GetChosenBranch(Config configuration)
        {
            var developBranchRegex = configuration.Branches[Config.DevelopBranchKey].Regex;
            var masterBranchRegex = configuration.Branches[Config.MasterBranchKey].Regex;

            var chosenBranch = repository.Branches.FirstOrDefault(b =>
                Regex.IsMatch(b.FriendlyName, developBranchRegex, RegexOptions.IgnoreCase) ||
                Regex.IsMatch(b.FriendlyName, masterBranchRegex, RegexOptions.IgnoreCase));

            return chosenBranch;
        }

        public List<Branch> GetBranchesForCommit(GitObject commit)
        {
            return repository.Branches.Where(b => !b.IsRemote && b.Tip == commit).ToList();
        }

        public List<Branch> GetExcludedInheritBranches(Config configuration)
        {
            return repository.Branches.Where(b =>
            {
                var branchConfig = configuration.GetConfigForBranch(b.NameWithoutRemote());

                return branchConfig == null || branchConfig.Increment == IncrementStrategy.Inherit;
            }).ToList();
        }

        public IEnumerable<Branch> GetReleaseBranches(IEnumerable<KeyValuePair<string, BranchConfig>> releaseBranchConfig)
        {
            return repository.Branches
                .Where(b => releaseBranchConfig.Any(c => Regex.IsMatch(b.FriendlyName, c.Value.Regex)));
        }

        public IEnumerable<Branch> ExcludingBranches(IEnumerable<Branch> branchesToExclude)
        {
            return repository.Branches.ExcludingBranches(branchesToExclude);
        }

        // TODO Should we cache this?
        public IEnumerable<Branch> GetBranchesContainingCommit(Commit commit, IEnumerable<Branch> branches = null, bool onlyTrackedBranches = false)
        {
            branches ??= repository.Branches.ToList();
            static bool IncludeTrackedBranches(Branch branch, bool includeOnlyTracked) => includeOnlyTracked && branch.IsTracking || !includeOnlyTracked;

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
                    log.Info($"Direct branch found: '{branch.FriendlyName}'.");
                    yield return branch;
                }

                if (directBranchHasBeenFound)
                {
                    yield break;
                }

                log.Info($"No direct branches found, searching through {(onlyTrackedBranches ? "tracked" : "all")} branches.");
                foreach (var branch in branchList.Where(b => IncludeTrackedBranches(b, onlyTrackedBranches)))
                {
                    log.Info($"Searching for commits reachable from '{branch.FriendlyName}'.");

                    var commits = GetCommitsReacheableFrom(commit, branch);

                    if (!commits.Any())
                    {
                        log.Info($"The branch '{branch.FriendlyName}' has no matching commits.");
                        continue;
                    }

                    log.Info($"The branch '{branch.FriendlyName}' has a matching commit.");
                    yield return branch;
                }
            }
        }

        public Dictionary<string, List<Branch>> GetMainlineBranches(Commit commit, IEnumerable<KeyValuePair<string, BranchConfig>> mainlineBranchConfigs)
        {
            return repository.Branches
                .Where(b =>
                {
                    return mainlineBranchConfigs.Any(c => Regex.IsMatch(b.FriendlyName, c.Value.Regex));
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
        public BranchCommit FindCommitBranchWasBranchedFrom(Branch branch, Config configuration, params Branch[] excludedBranches)
        {
            if (branch == null)
            {
                throw new ArgumentNullException(nameof(branch));
            }

            using (log.IndentLog($"Finding branch source of '{branch.FriendlyName}'"))
            {
                if (branch.Tip == null)
                {
                    log.Warning(String.Format(MissingTipFormat, branch.FriendlyName));
                    return BranchCommit.Empty;
                }

                var possibleBranches = GetMergeCommitsForBranch(branch, configuration, excludedBranches)
                    .Where(b => !branch.IsSameBranch(b.Branch))
                    .ToList();

                if (possibleBranches.Count > 1)
                {
                    var first = possibleBranches.First();
                    log.Info($"Multiple source branches have been found, picking the first one ({first.Branch.FriendlyName}).{System.Environment.NewLine}" +
                             $"This may result in incorrect commit counting.{System.Environment.NewLine}Options were:{System.Environment.NewLine}" +
                             String.Join(", ", possibleBranches.Select(b => b.Branch.FriendlyName)));
                    return first;
                }

                return possibleBranches.SingleOrDefault();
            }
        }


        public SemanticVersion GetCurrentCommitTaggedVersion(GitObject commit, EffectiveConfiguration config)
        {
            return repository.Tags
                .SelectMany(t =>
                {
                    if (t.PeeledTarget() == commit && SemanticVersion.TryParse(t.FriendlyName, config.GitTagPrefix, out var version))
                        return new[] {
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

        public IEnumerable<SemanticVersion> GetVersionTagsOnBranch(Branch branch, string tagPrefixRegex)
        {
            if (semanticVersionTagsOnBranchCache.ContainsKey(branch))
            {
                log.Debug($"Cache hit for version tags on branch '{branch.CanonicalName}");
                return semanticVersionTagsOnBranchCache[branch];
            }

            using (log.IndentLog($"Getting version tags from branch '{branch.CanonicalName}'."))
            {
                var tags = GetValidVersionTags(tagPrefixRegex);

                var versionTags = branch.Commits.SelectMany(c => tags.Where(t => c.Sha == t.Item1.Target.Sha).Select(t => t.Item2)).ToList();

                semanticVersionTagsOnBranchCache.Add(branch, versionTags);
                return versionTags;
            }
        }

        public IEnumerable<Tuple<Tag, SemanticVersion>> GetValidVersionTags(string tagPrefixRegex, DateTimeOffset? olderThan = null)
        {
            var tags = new List<Tuple<Tag, SemanticVersion>>();

            foreach (var tag in repository.Tags)
            {
                if (!(tag.PeeledTarget() is Commit commit) || (olderThan.HasValue && commit.When() > olderThan.Value))
                    continue;

                if (SemanticVersion.TryParse(tag.FriendlyName, tagPrefixRegex, out var semver))
                {
                    tags.Add(Tuple.Create(tag, semver));
                }
            }

            return tags;
        }


        public ICommitLog GetCommitLog(Commit baseVersionSource, Commit currentCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = currentCommit,
                ExcludeReachableFrom = baseVersionSource,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            return repository.Commits.QueryBy(filter);
        }

        public bool GetMatchingCommitBranch(Commit baseVersionSource, Branch branch, Commit firstMatchingCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = branch,
                ExcludeReachableFrom = baseVersionSource,
                FirstParentOnly = true,
            };
            var query = repository.Commits.QueryBy(filter);

            return query.Contains(firstMatchingCommit);
        }

        public string ShortenObjectId(GitObject commit)
        {
            return repository.ObjectDatabase.ShortenObjectId(commit);
        }

        public VersionField? DetermineIncrementedField(BaseVersion baseVersion, GitVersionContext context)
        {
            return IncrementStrategyFinder.DetermineIncrementedField(repository, context, baseVersion);
        }

        private Commit GetForwardMerge(Commit commitToFindCommonBase, Commit findMergeBase)
        {
            var forwardMerge = repository.Commits
                .QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = commitToFindCommonBase,
                    ExcludeReachableFrom = findMergeBase
                })
                .FirstOrDefault(c => c.Parents.Contains(findMergeBase));
            return forwardMerge;
        }

        private IEnumerable<Commit> GetCommitsReacheableFrom(Commit commit, Branch branch)
        {
            var commits = repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = branch
            }).Where(c => c.Sha == commit.Sha);
            return commits;
        }
        private IEnumerable<BranchCommit> GetMergeCommitsForBranch(Branch branch, Config configuration, IEnumerable<Branch> excludedBranches)
        {
            if (mergeBaseCommitsCache.ContainsKey(branch))
            {
                log.Debug($"Cache hit for getting merge commits for branch {branch.CanonicalName}.");
                return mergeBaseCommitsCache[branch];
            }

            var currentBranchConfig = configuration.GetConfigForBranch(branch.NameWithoutRemote());
            var regexesToCheck = currentBranchConfig == null
                ? new[] { ".*" } // Match anything if we can't find a branch config
                : currentBranchConfig.SourceBranches.Select(sb => configuration.Branches[sb].Regex);
            var branchMergeBases = ExcludingBranches(excludedBranches)
                .Where(b =>
                {
                    if (b == branch) return false;
                    var branchCanBeMergeBase = regexesToCheck.Any(regex => Regex.IsMatch(b.FriendlyName, regex));

                    return branchCanBeMergeBase;
                })
                .Select(otherBranch =>
                {
                    if (otherBranch.Tip == null)
                    {
                        log.Warning(String.Format(MissingTipFormat, otherBranch.FriendlyName));
                        return BranchCommit.Empty;
                    }

                    var findMergeBase = FindMergeBase(branch, otherBranch);
                    return new BranchCommit(findMergeBase, otherBranch);
                })
                .Where(b => b.Commit != null)
                .OrderByDescending(b => b.Commit.Committer.When)
                .ToList();
            mergeBaseCommitsCache.Add(branch, branchMergeBases);

            return branchMergeBases;
        }

        private class MergeBaseData
        {
            public Commit MergeBase { get; }

            public MergeBaseData(Commit mergeBase)
            {
                MergeBase = mergeBase;
            }
        }
    }
}
