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

        public static Commit FindCommitBranchWasBranchedFrom(this Branch branch, IRepository repository)
        {
            var tips = repository.Branches.Select(b => b.Tip).Where(c => c.Sha != branch.Tip.Sha).ToList();
            return repository.Commits.FirstOrDefault(c => tips.Contains(c) || c.Parents.Count() > 1) ?? branch.Tip;
        }

        public static IEnumerable<Tag> TagsByDate(this IRepository repository, Commit commit)
        {
            return repository.Tags
                .Where(tag => tag.PeeledTarget() == commit)
                .OrderByDescending(tag =>
                {
                    if (tag.Annotation != null)
                    {
                        return tag.Annotation.Tagger.When;
                    }
                    //lightweight tags will not have an Annotation
                    return commit.Committer.When;
                });
        }

        public static IEnumerable<Tag> SemVerTagsRelatedToVersion(this IRepository repository, EffectiveConfiguration configuration, SemanticVersion version)
        {
            foreach (var tag in repository.Tags)
            {
                SemanticVersion tagVersion;
                if (SemanticVersion.TryParse(tag.Name, configuration.GitTagPrefix, out tagVersion))
                {
                    if (version.Major == tagVersion.Major &&
                        version.Minor == tagVersion.Minor &&
                        version.Patch == tagVersion.Patch)
                    {
                        yield return tag;
                    }
                }
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