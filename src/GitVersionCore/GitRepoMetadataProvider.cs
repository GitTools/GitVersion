using JetBrains.Annotations;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion
{
    public class GitRepoMetadataProvider
    {
        private Dictionary<Branch, List<BranchCommit>> mergeBaseCommitsCache;
        private Dictionary<Tuple<Branch, Branch>, MergeBaseData> mergeBaseCache;
        private Dictionary<Branch, List<SemanticVersion>> semanticVersionTagsOnBranchCache;
        private IRepository repository;
        const string missingTipFormat = "{0} has no tip. Please see http://example.com/docs for information on how to fix this.";


        public GitRepoMetadataProvider(IRepository repository)
        {
            mergeBaseCache = new Dictionary<Tuple<Branch, Branch>, MergeBaseData>();
            mergeBaseCommitsCache = new Dictionary<Branch, List<BranchCommit>>();
            semanticVersionTagsOnBranchCache = new Dictionary<Branch, List<SemanticVersion>>();
            this.repository = repository;
        }

        public IEnumerable<SemanticVersion> GetVersionTagsOnBranch(Branch branch, IRepository repository, string tagPrefixRegex)
        {
            if (semanticVersionTagsOnBranchCache.ContainsKey(branch))
            {
                Logger.WriteDebug(string.Format("Cache hit for version tags on branch '{0}", branch.CanonicalName));
                return semanticVersionTagsOnBranchCache[branch];
            }

            using (Logger.IndentLog(string.Format("Getting version tags from branch '{0}'.", branch.CanonicalName)))
            {
                var tags = repository.Tags.Select(t => t).ToList();

                var versionTags = repository.Commits.QueryBy(new CommitFilter
                {
                    IncludeReachableFrom = branch.Tip
                })
                .SelectMany(c => tags.Where(t => c.Sha == t.Target.Sha).SelectMany(t =>
                {
                    SemanticVersion semver;
                    if (SemanticVersion.TryParse(t.FriendlyName, tagPrefixRegex, out semver))
                        return new[] { semver };
                    return new SemanticVersion[0];
                })).ToList();

                semanticVersionTagsOnBranchCache.Add(branch, versionTags);
                return versionTags;
            }
        }

        // TODO Should we cache this?
        public IEnumerable<Branch> GetBranchesContainingCommit([NotNull] Commit commit, IRepository repository, IList<Branch> branches, bool onlyTrackedBranches)
        {
            if (commit == null)
            {
                throw new ArgumentNullException("commit");
            }
            Logger.WriteDebug("Heh");
            using (Logger.IndentLog(string.Format("Getting branches containing the commit '{0}'.", commit.Id)))
            {
                var directBranchHasBeenFound = false;
                Logger.WriteInfo("Trying to find direct branches.");
                // TODO: It looks wasteful looping through the branches twice. Can't these loops be merged somehow? @asbjornu
                foreach (var branch in branches)
                {
                    if (branch.Tip != null && branch.Tip.Sha != commit.Sha || (onlyTrackedBranches && !branch.IsTracking))
                    {
                        continue;
                    }

                    directBranchHasBeenFound = true;
                    Logger.WriteInfo(string.Format("Direct branch found: '{0}'.", branch.FriendlyName));
                    yield return branch;
                }

                if (directBranchHasBeenFound)
                {
                    yield break;
                }

                Logger.WriteInfo(string.Format("No direct branches found, searching through {0} branches.", onlyTrackedBranches ? "tracked" : "all"));
                foreach (var branch in branches.Where(b => onlyTrackedBranches && !b.IsTracking))
                {
                    Logger.WriteInfo(string.Format("Searching for commits reachable from '{0}'.", branch.FriendlyName));

                    var commits = repository.Commits.QueryBy(new CommitFilter
                    {
                        IncludeReachableFrom = branch
                    }).Where(c => c.Sha == commit.Sha);

                    if (!commits.Any())
                    {
                        Logger.WriteInfo(string.Format("The branch '{0}' has no matching commits.", branch.FriendlyName));
                        continue;
                    }

                    Logger.WriteInfo(string.Format("The branch '{0}' has a matching commit.", branch.FriendlyName));
                    yield return branch;
                }
            }
        }

        /// <summary>
        /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
        /// </summary>
        public Commit FindMergeBase(Branch branch, Branch otherBranch, IRepository repository)
        {
            var key = Tuple.Create(branch, otherBranch);

            if (mergeBaseCache.ContainsKey(key))
            {
                Logger.WriteDebug(string.Format(
                    "Cache hit for merge base between '{0}' and '{1}'.",
                    branch.FriendlyName, otherBranch.FriendlyName));
                return mergeBaseCache[key].MergeBase;
            }

            using (Logger.IndentLog(string.Format("Finding merge base between '{0}' and '{1}'.", branch.FriendlyName, otherBranch.FriendlyName)))
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
                    Logger.WriteInfo(string.Format("Found merge base of {0}", findMergeBase.Sha));
                    // We do not want to include merge base commits which got forward merged into the other branch
                    bool mergeBaseWasForwardMerge;
                    do
                    {
                        // Now make sure that the merge base is not a forward merge
                        mergeBaseWasForwardMerge = otherBranch.Commits
                            .SkipWhile(c => c != commitToFindCommonBase)
                            .TakeWhile(c => c != findMergeBase)
                            .Any(c => c.Parents.Contains(findMergeBase));
                        if (mergeBaseWasForwardMerge)
                        {
                            var second = commitToFindCommonBase.Parents.First();
                            var mergeBase = repository.ObjectDatabase.FindMergeBase(commit, second);
                            if (mergeBase == findMergeBase)
                            {
                                break;
                            }
                            findMergeBase = mergeBase;
                            Logger.WriteInfo(string.Format("Merge base was due to a forward merge, next merge base is {0}", findMergeBase));
                        }
                    } while (mergeBaseWasForwardMerge);
                }

                // Store in cache.
                mergeBaseCache.Add(key, new MergeBaseData(branch, otherBranch, repository, findMergeBase));

                return findMergeBase;
            }
        }

        /// <summary>
        /// Find the commit where the given branch was branched from another branch.
        /// If there are multiple such commits and branches, returns the newest commit.
        /// </summary>
        public BranchCommit FindCommitBranchWasBranchedFrom([NotNull] Branch branch, IRepository repository, params Branch[] excludedBranches)
        {
            if (branch == null)
            {
                throw new ArgumentNullException("branch");
            }

            using (Logger.IndentLog(string.Format("Finding branch source of '{0}'", branch.FriendlyName)))
            {
                if (branch.Tip == null)
                {
                    Logger.WriteWarning(string.Format(missingTipFormat, branch.FriendlyName));
                    return BranchCommit.Empty;
                }

                return GetMergeCommitsForBranch(branch).ExcludingBranches(excludedBranches).FirstOrDefault(b => !branch.IsSameBranch(b.Branch));
            }
        }


        List<BranchCommit> GetMergeCommitsForBranch(Branch branch)
        {
            if (mergeBaseCommitsCache.ContainsKey(branch))
            {
                Logger.WriteDebug(string.Format(
                    "Cache hit for getting merge commits for branch {0}.",
                    branch.CanonicalName));
                return mergeBaseCommitsCache[branch];
            }

            var branchMergeBases = repository.Branches.Select(otherBranch =>
            {
                if (otherBranch.Tip == null)
                {
                    Logger.WriteWarning(string.Format(missingTipFormat, otherBranch.FriendlyName));
                    return BranchCommit.Empty;
                }

                var findMergeBase = FindMergeBase(branch, otherBranch, repository);
                return new BranchCommit(findMergeBase, otherBranch);
            }).Where(b => b.Commit != null).OrderByDescending(b => b.Commit.Committer.When).ToList();
            mergeBaseCommitsCache.Add(branch, branchMergeBases);

            return branchMergeBases;
        }

        private class MergeBaseData
        {
            public Branch Branch { get; private set; }
            public Branch OtherBranch { get; private set; }
            public IRepository Repository { get; private set; }

            public Commit MergeBase { get; private set; }

            public MergeBaseData(Branch branch, Branch otherBranch, IRepository repository, Commit mergeBase)
            {
                Branch = branch;
                OtherBranch = otherBranch;
                Repository = repository;
                MergeBase = mergeBase;
            }
        }
    }
}