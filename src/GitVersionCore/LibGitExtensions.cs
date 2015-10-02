namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using GitVersion.Helpers;
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

        public static SemanticVersion LastVersionTagOnBranch(this Branch branch, IRepository repository, string tagPrefixRegex)
        {
            var tags = repository.Tags.Select(t => t).ToList();

            return repository.Commits.QueryBy(new CommitFilter
            {
                Since = branch.Tip
            })
            .SelectMany(c => tags.Where(t => c.Sha == t.Target.Sha).SelectMany(t =>
            {
                SemanticVersion semver;
                if (SemanticVersion.TryParse(t.Name, tagPrefixRegex, out semver))
                    return new [] { semver };
                return new SemanticVersion[0];
            }))
            .FirstOrDefault();
        }

        public static Commit FindCommitBranchWasBranchedFrom(this Branch branch, IRepository repository, params Branch[] excludedBranches)
        {
            using (Logger.IndentLog("Finding branch source"))
            {
                var otherBranches = repository.Branches.Except(excludedBranches).Where(b => IsSameBranch(branch, b)).ToList();
                var mergeBases = otherBranches.Select(b =>
                {
                    var otherCommit = b.Tip;
                    if (b.Tip.Parents.Contains(branch.Tip))
                    {
                        otherCommit = b.Tip.Parents.First();
                    }
                    var mergeBase = repository.Commits.FindMergeBase(otherCommit, branch.Tip);
                    return mergeBase;
                }).Where(b => b != null).ToList();
                return mergeBases.OrderByDescending(b => b.Committer.When).FirstOrDefault();
            }
        }

        static bool IsSameBranch(Branch branch, Branch b)
        {
            return (b.IsRemote ? b.Name.Replace(b.Remote.Name + "/", string.Empty) : b.Name) != branch.Name;
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

            foreach (var branch in repository.Branches.Where(b => (onlyTrackedBranches && !b.IsTracking)))
            {
                var commits = repository.Commits.QueryBy(new CommitFilter { Since = branch }).Where(c => c.Sha == commit.Sha);

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
                    @"log --graph --format=""%h %cr %d"" --decorate --date=relative --all --remotes=*" + (maxCommits != null ? string.Format(" -n {0}", maxCommits) : null),
                    //@"log --graph --abbrev-commit --decorate --date=relative --all --remotes=*",
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

            if (writer != null) writer(output.ToString());
            else Console.Write(output.ToString());
        }
    }
}