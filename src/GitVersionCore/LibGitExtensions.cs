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

        /// <summary>
        /// Checks if the two branch objects refer to the same branch.
        /// If the two branches are the local and (tracking) remote, they are also considered the same.
        /// </summary>
        public static bool IsSameBranch(this Branch branch, Branch otherBranch)
        {
            return branch.GetComparisonBranchName() == otherBranch.GetComparisonBranchName();
        }

        /// <summary>
        /// For comparison, find the "best" branch name,
        /// either the name of the remote (tracked) branch, or the local branch name.
        /// </summary>
        public static string GetComparisonBranchName(this Branch branch)
        {
            // There are several possibilities of the state of a branch:
            //   1. local branch, tracks a remote branch with same name
            //   2. local branch, tracks a remote branch with *different* name
            //   3. local branch, without a remote branch
            //   4. remote branch, for which a local tracking branch exists
            //   5. remote branch, for which no local tracking branch exists
            // branch.UpstreamBranchCanonicalName - Cases 1,2,4,5: 'refs/heads/[remote-branch-name]'; Case 3: null
            // branch.FriendlyName - Cases 1-3: '[local-branch-name]'; Cases 4,5: '{branch.RemoteName}/[remote-branch-name]'
            //
            // We want the the branch name itself, and the remote name should win over the local name,
            // since the local and remote version of the branch should be the same.
            // Thus, we'll use UpstreamBranchCanonicalName, stripping the /refs/heads/ prefix.
            // If that is null, we use FriendlyName instead.
            var upstreamName = branch.UpstreamBranchCanonicalName;
            if (upstreamName == null)
            {
                return branch.FriendlyName;
            }

            return upstreamName.Substring("refs/heads/".Length);
        }

        /// <summary>
        /// Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<BranchCommit> ExcludingBranches([NotNull] this IEnumerable<BranchCommit> branches, [NotNull] IEnumerable<Branch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !IsSameBranch(b.Branch, bte)));
        }

        /// <summary>
        /// Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<Branch> ExcludingBranches([NotNull] this IEnumerable<Branch> branches, [NotNull] IEnumerable<Branch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !IsSameBranch(b, bte)));
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
                    using (var stream = ((Blob)treeEntry.Target).GetContentStream())
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
