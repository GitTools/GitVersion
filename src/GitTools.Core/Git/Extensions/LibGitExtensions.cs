namespace GitTools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using JetBrains.Annotations;

    using LibGit2Sharp;
    using Logging;

    public static class LibGitExtensions
    {
        static readonly ILog Log = LogProvider.GetLogger(typeof(LibGitExtensions));

        public static DateTimeOffset When(this Commit commit)
        {
            return commit.Committer.When;
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

        static bool IsSameBranch(Branch branch, Branch b)
        {
            return (b.IsRemote ? b.FriendlyName.Replace(b.RemoteName + "/", string.Empty) : b.FriendlyName) != branch.FriendlyName;
        }

        public static IEnumerable<Branch> GetBranchesContainingCommit([NotNull] this Commit commit, IRepository repository, IList<Branch> branches, bool onlyTrackedBranches)
        {
            if (commit == null)
            {
                throw new ArgumentNullException("commit");
            }

            var directBranchHasBeenFound = false;
            foreach (var branch in branches)
            {
                if (branch.Tip != null && branch.Tip.Sha != commit.Sha || (onlyTrackedBranches && !branch.IsTracking))
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

            foreach (var branch in branches.Where(b => (onlyTrackedBranches && !b.IsTracking)))
            {
                var commits = repository.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = branch }).Where(c => c.Sha == commit.Sha);

                if (!commits.Any())
                {
                    continue;
                }

                yield return branch;
            }
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

            Log.Info("Checking out files that might be needed later in dynamic repository");

            foreach (var fileName in fileNames)
            {
                try
                {
                    Log.Info(string.Format("  Trying to check out '{0}'", fileName));

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
                    Log.Warn(string.Format("  An error occurred while checking out '{0}': '{1}'", fileName, ex.Message));
                }
            }
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

        public static string CreateGitLogArgs(int? maxCommits)
        {
            return @"log --graph --format=""%h %cr %d"" --decorate --date=relative --all --remotes=*" + (maxCommits != null ? string.Format(" -n {0}", maxCommits) : null);
        }
    }
}