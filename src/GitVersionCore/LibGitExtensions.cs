namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using GitVersion.Helpers;
    using LibGit2Sharp;

    public static class LibGitExtensions
    {
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

        public static Branch FindBranch(this IRepository repository, string branchName)
        {
            var exact = repository.Branches.FirstOrDefault(x => x.FriendlyName == branchName);
            if (exact != null)
            {
                return exact;
            }

            return repository.Branches.FirstOrDefault(x => x.FriendlyName == "origin/" + branchName);
        }

        public static void DumpGraph(this IRepository repository, Action<string> writer = null, int? maxCommits = null)
        {
            DumpGraph(repository.Info.Path, writer, maxCommits);
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
    }
}
