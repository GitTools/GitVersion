using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion.Extensions
{
    public static class LibGitExtensions
    {
        public static TResult WithRepository<TResult>(this string dotGitDirectory, Func<IRepository, TResult> action)
        {
            using var repo = new Repository(dotGitDirectory);
            return action(repo);
        }

        public static DateTimeOffset When(this Commit commit)
        {
            return commit.Committer.When;
        }

        public static string NameWithoutRemote(this Branch branch)
        {
            return branch.IsRemote
                ? branch.FriendlyName.Substring(branch.FriendlyName.IndexOf("/", StringComparison.Ordinal) + 1)
                : branch.FriendlyName;
        }

        public static string NameWithoutOrigin(this Branch branch)
        {
            return branch.IsRemote && branch.FriendlyName.StartsWith("origin/")
                ? branch.FriendlyName.Substring("origin/".Length)
                : branch.FriendlyName;
        }

        /// <summary>
        /// Checks if the two branch objects refer to the same branch (have the same friendly name).
        /// </summary>
        public static bool IsSameBranch(this Branch branch, Branch otherBranch)
        {
            // For each branch, fixup the friendly name if the branch is remote.
            var otherBranchFriendlyName = otherBranch.NameWithoutRemote();
            var branchFriendlyName = branch.NameWithoutRemote();

            return otherBranchFriendlyName == branchFriendlyName;
        }

        /// <summary>
        /// Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<BranchCommit> ExcludingBranches(this IEnumerable<BranchCommit> branches, IEnumerable<Branch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !IsSameBranch(b.Branch, bte)));
        }

        /// <summary>
        /// Exclude the given branches (by value equality according to friendly name).
        /// </summary>
        public static IEnumerable<Branch> ExcludingBranches( this IEnumerable<Branch> branches, IEnumerable<Branch> branchesToExclude)
        {
            return branches.Where(b => branchesToExclude.All(bte => !IsSameBranch(b, bte)));
        }

        public static GitObject PeeledTarget(this Tag tag)
        {
            var target = tag.Target;

            while (target is TagAnnotation annotation)
            {
                target = annotation.Target;
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

        public static Branch FindBranch(this IRepository repository, string branchName)
        {
            return repository.Branches.FirstOrDefault(x => x.NameWithoutRemote() == branchName);
        }

        public static void DumpGraph(this IRepository repository, Action<string> writer = null, int? maxCommits = null)
        {
            DumpGraph(repository.Info.Path, writer, maxCommits);
        }

        public static void DumpGraph(string workingDirectory, Action<string> writer = null, int? maxCommits = null)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            var output = new StringBuilder();
            try
            {
                ProcessHelper.Run(
                    o => output.AppendLine(o),
                    e => output.AppendLineFormat("ERROR: {0}", e),
                    null,
                    "git",
                    GitRepositoryHelper.CreateGitLogArgs(maxCommits),
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
    }
}
