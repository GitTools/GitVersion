namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using JetBrains.Annotations;

    using LibGit2Sharp;

    static class LibGitExtensions
    {
        public static DateTimeOffset When(this Commit commit)
        {
            return commit.Committer.When;
        }

        public static IEnumerable<SemanticVersion> GetVersionTagsOnBranch(this Branch branch, IRepository repository, string tagPrefixRegex)
        {
            var tags = repository.Tags.Select(t => t).ToList();

            return repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = branch.Tip
            })
            .SelectMany(c => tags.Where(t => c.Sha == t.Target.Sha).SelectMany(t =>
            {
                SemanticVersion semver;
                if (SemanticVersion.TryParse(t.FriendlyName, tagPrefixRegex, out semver))
                    return new [] { semver };
                return new SemanticVersion[0];
            }));
        }


        public static Commit FindCommitBranchWasBranchedFrom([NotNull] this Branch branch, IRepository repository, params Branch[] excludedBranches)
        {
            const string missingTipFormat = "{0} has no tip. Please see http://example.com/docs for information on how to fix this.";

            if (branch == null)
            {
                throw new ArgumentNullException("branch");
            }

            using (Logger.IndentLog("Finding branch source"))
            {
                if (branch.Tip == null)
                {
                    Logger.WriteWarning(string.Format(missingTipFormat, branch.FriendlyName));
                    return null;
                }

                var otherBranches = repository.Branches
                    .Except(excludedBranches)
                    .Where(b => IsSameBranch(branch, b))
                    .ToList();
                var mergeBases = otherBranches.Select(otherBranch =>
                {
                    if (otherBranch.Tip == null)
                    {
                        Logger.WriteWarning(string.Format(missingTipFormat, otherBranch.FriendlyName));
                        return null;
                    }

                    var findMergeBase = FindMergeBase(branch, otherBranch, repository);
                    return new
                    {
                        mergeBaseCommit = findMergeBase,
                        branch = otherBranch
                    };
                }).Where(b => b != null).OrderByDescending(b => b.mergeBaseCommit.Committer.When).ToList();

                var firstOrDefault = mergeBases.FirstOrDefault();
                if (firstOrDefault != null)
                {
                    return firstOrDefault.mergeBaseCommit;
                }
                return null;
            }
        }

        public static Commit FindMergeBase(this Branch branch, Branch otherBranch, IRepository repository)
        {
            using (Logger.IndentLog(string.Format("Finding merge base between '{0}' and {1}.", branch.FriendlyName, otherBranch.FriendlyName)))
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
                    bool mergeBaseWasFowardMerge;
                    do
                    {
                        // Now make sure that the merge base is not a forward merge
                        mergeBaseWasFowardMerge = otherBranch.Commits
                            .SkipWhile(c => c != commitToFindCommonBase)
                            .TakeWhile(c => c != findMergeBase)
                            .Any(c => c.Parents.Contains(findMergeBase));
                        if (mergeBaseWasFowardMerge)
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
                    } while (mergeBaseWasFowardMerge);
                }
                return findMergeBase;
            }
        }

        static bool IsSameBranch(Branch branch, Branch b)
        {
            return (b.IsRemote ? 
                b.FriendlyName.Substring(b.FriendlyName.IndexOf("/", StringComparison.Ordinal) + 1) : 
                b.FriendlyName) != branch.FriendlyName;
        }

        public static IEnumerable<Branch> GetBranchesContainingCommit([NotNull] this Commit commit, IRepository repository, IList<Branch> branches, bool onlyTrackedBranches)
        {
            if (commit == null)
            {
                throw new ArgumentNullException("commit");
            }

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

        private static Dictionary<string, GitObject> _cachedPeeledTarget = new Dictionary<string, GitObject>();

        public static GitObject PeeledTarget(this Tag tag)
        {
            GitObject cachedTarget;
            if(_cachedPeeledTarget.TryGetValue(tag.Target.Sha, out cachedTarget))
            {
                return cachedTarget;
            }
            var target = tag.Target;

            while (target is TagAnnotation)
            {
                target = ((TagAnnotation)(target)).Target;
            }
            _cachedPeeledTarget.Add(tag.Target.Sha, target);
            return target;
        }

        public static IEnumerable<Commit> CommitsPriorToThan(this Branch branch, DateTimeOffset olderThan)
        {
            return branch.Commits.SkipWhile(c => c.When() > olderThan);
        }

        public static bool IsDetachedHead(this Branch branch)
        {
            return branch.CanonicalName.Equals("(no branch)", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetRepositoryDirectory(this IRepository repository, bool omitGitPostFix = true)
        {
            var gitDirectory = repository.Info.Path;

            gitDirectory = gitDirectory.TrimEnd(Path.DirectorySeparatorChar);

            if (omitGitPostFix && gitDirectory.EndsWith(".git"))
            {
                gitDirectory = gitDirectory.Substring(0, gitDirectory.Length - ".git".Length);
                gitDirectory = gitDirectory.TrimEnd(Path.DirectorySeparatorChar);
            }

            return gitDirectory;
        }

        public static void CheckoutFilesIfExist(this IRepository repository, params string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0)
            {
                return;
            }

            Logger.WriteInfo("Checking out files that might be needed later in dynamic repository");

            foreach (var fileName in fileNames)
            {
                try
                {
                    Logger.WriteInfo(string.Format("  Trying to check out '{0}'", fileName));

                    var headBranch = repository.Head;
                    var tip = headBranch.Tip;

                    var treeEntry = tip[fileName];
                    if (treeEntry == null)
                    {
                        continue;
                    }

                    var fullPath = Path.Combine(repository.GetRepositoryDirectory(), fileName);
                    using (var stream = ((Blob) treeEntry.Target).GetContentStream())
                    {
                        using (var streamReader = new BinaryReader(stream))
                        {
                            File.WriteAllBytes(fullPath, streamReader.ReadBytes((int)stream.Length));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteWarning(string.Format("  An error occurred while checking out '{0}': '{1}'", fileName, ex.Message));
                }
            }
        }
    }
}
