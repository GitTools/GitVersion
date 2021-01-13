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
        /// <summary>
        ///     Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<BranchCommit> ExcludingBranches(this IEnumerable<BranchCommit> branches, IEnumerable<IBranch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !b.Branch.IsSameBranch(bte)));
        }

        /// <summary>
        ///     Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<IBranch> ExcludingBranches(this IEnumerable<IBranch> branches, IEnumerable<IBranch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !b.IsSameBranch(bte)));
        }
        public static IEnumerable<ICommit> CommitsPriorToThan(this IBranch branch, DateTimeOffset olderThan)
        {
            return branch.Commits.SkipWhile(c => c.CommitterWhen > olderThan);
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
            if (string.Equals(branchName, branchNameToCompareAgainst, StringComparison.OrdinalIgnoreCase))
                return true;

            // "refs/head/develop" == "develop"
            return branchName.EndsWith($"/{branchNameToCompareAgainst}", StringComparison.OrdinalIgnoreCase);

        }

        public static string CreateGitLogArgs(int? maxCommits)
        {
            return @"log --graph --format=""%h %cr %d"" --decorate --date=relative --all --remotes=*" + (maxCommits != null ? $" -n {maxCommits}" : null);
        }
    }
}
