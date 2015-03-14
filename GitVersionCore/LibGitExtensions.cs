namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using LibGit2Sharp;

    static class LibGitExtensions
    {
        public static DateTimeOffset When(this Commit commit)
        {
            return commit.Committer.When;
        }

        public static Branch FindBranch(this IRepository repository, string branchName)
        {
            var exact = repository.Branches.FirstOrDefault(x => x.Name == branchName);
            if (exact != null)
            {
                return exact;
            }

            return repository.Branches.FirstOrDefault(x => x.Name == "origin/" + branchName);
        }

        public static Commit FindCommitBranchWasBranchedFrom(this Branch branch, IRepository repository, bool onlyTrackedBranches, params Branch[] excludedBranches)
        {
            var currentBranches = branch.Tip.GetBranchesContainingCommit(repository, onlyTrackedBranches).ToList();
            var tips = repository.Branches.Except(excludedBranches).Where(b => b != branch && !b.IsRemote).Select(b => b.Tip).ToList();
            var branchPoint = branch.Commits.FirstOrDefault(c =>
            {
                if (tips.Contains(c)) return true;
                var branchesContainingCommit = c.GetBranchesContainingCommit(repository, onlyTrackedBranches).ToList();
                return branchesContainingCommit.Count > currentBranches.Count;
            });
            return branchPoint ?? branch.Tip;
        }

        public static IEnumerable<Branch> GetBranchesContainingCommit(this Commit commit, IRepository repository, bool onlyTrackedBranches)
        {
            var directBranchHasBeenFound = false;
            foreach (var branch in repository.Branches)
            {
                if (branch.Tip.Sha != commit.Sha || (onlyTrackedBranches && !branch.IsTracking))
                {
                    continue;
                }

                directBranchHasBeenFound = true;
                yield return branch;
            }

            if (directBranchHasBeenFound)
            {
                yield break;
            }

            foreach (var branch in repository.Branches)
            {
                var commits = repository.Commits.QueryBy(new CommitFilter { Since = branch }).Where(c => c.Sha == commit.Sha);

                if (!commits.Any())
                {
                    continue;
                }

                yield return branch;
            }
        }

        public static bool IsDirectMergeFromCommit(this Tag tag, Commit commit)
        {
            var targetCommit = tag.Target as Commit;
            if (targetCommit != null)
            {
                var parents = targetCommit.Parents;
                if (parents != null)
                {
                    foreach (var parent in parents)
                    {
                        if (parent != null)
                        {
                            if (string.Equals(parent.Id.Sha, commit.Id.Sha, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static GitObject PeeledTarget(this Tag tag)
        {
            var target = tag.Target;

            while (target is TagAnnotation)
            {
                target = ((TagAnnotation)(target)).Target;
            }

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

            gitDirectory = gitDirectory.TrimEnd('\\');

            if (omitGitPostFix && gitDirectory.EndsWith(".git"))
            {
                gitDirectory = gitDirectory.Substring(0, gitDirectory.Length - ".git".Length);
                gitDirectory = gitDirectory.TrimEnd('\\');
            }

            return gitDirectory;
        }

        public static void CheckoutFilesIfExist(this IRepository repository, params string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0)
            {
                return;
            }

            Logger.WriteInfo(string.Format("Checking out files that might be needed later in dynamic repository"));

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