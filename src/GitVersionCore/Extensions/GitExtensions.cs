using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GitVersion.Helpers;

namespace GitVersion.Extensions
{
    public static class GitExtensions
    {
        public static DateTimeOffset When(this ICommit commit)
        {
            return commit.CommitterWhen.Value;
        }

        public static string NameWithoutRemote(this IBranch branch)
        {
            return branch.IsRemote
                ? branch.FriendlyName.Substring(branch.FriendlyName.IndexOf("/", StringComparison.Ordinal) + 1)
                : branch.FriendlyName;
        }

        public static string NameWithoutOrigin(this IBranch branch)
        {
            return branch.IsRemote && branch.FriendlyName.StartsWith("origin/")
                ? branch.FriendlyName.Substring("origin/".Length)
                : branch.FriendlyName;
        }

        /// <summary>
        /// Checks if the two branch objects refer to the same branch (have the same friendly name).
        /// </summary>
        public static bool IsSameBranch(this IBranch branch, IBranch otherBranch)
        {
            // For each branch, fixup the friendly name if the branch is remote.
            var otherBranchFriendlyName = otherBranch.NameWithoutRemote();
            var branchFriendlyName = branch.NameWithoutRemote();

            return otherBranchFriendlyName.IsEquivalentTo(branchFriendlyName);
        }

        /// <summary>
        /// Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<BranchCommit> ExcludingBranches(this IEnumerable<BranchCommit> branches, IEnumerable<IBranch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !IsSameBranch(b.Branch, bte)));
        }

        /// <summary>
        /// Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<IBranch> ExcludingBranches(this IEnumerable<IBranch> branches, IEnumerable<IBranch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !IsSameBranch(b, bte)));
        }
        public static IEnumerable<ICommit> CommitsPriorToThan(this IBranch branch, DateTimeOffset olderThan)
        {
            return branch.Commits.SkipWhile(c => c.When() > olderThan);
        }

        public static bool IsDetachedHead(this IBranch branch)
        {
            return branch.CanonicalName.Equals("(no branch)", StringComparison.OrdinalIgnoreCase);
        }

        public static void DumpGraph(string workingDirectory, Action<string> writer = null, int? maxCommits = null)
        {
            var output = new StringBuilder();
            try
            {
                ProcessHelper.Run(
                    o => output.AppendLine(o),
                    e => output.AppendLineFormat("ERROR: {0}", e),
                    null,
                    "git",
                    CreateGitLogArgs(maxCommits),
                    workingDirectory);
            }
            catch (FileNotFoundException exception)
            {
                if (exception.FileName != "git")
                {
                    throw;
                }

                output.AppendLine("Could not execute 'git log' due to the following error:");
                output.AppendLine(exception.ToString());
            }

            if (writer != null)
            {
                writer(output.ToString());
            }
            else
            {
                Console.Write(output.ToString());
            }
        }

        public static bool IsBranch(this string branchName, string branchNameToCompareAgainst)
        {
            // "develop" == "develop"
            if (String.Equals(branchName, branchNameToCompareAgainst, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // "refs/head/develop" == "develop"
            if (branchName.EndsWith($"/{branchNameToCompareAgainst}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static string CreateGitLogArgs(int? maxCommits)
        {
            return @"log --graph --format=""%h %cr %d"" --decorate --date=relative --all --remotes=*" + (maxCommits != null ? $" -n {maxCommits}" : null);
        }

    }
}
