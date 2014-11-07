namespace GitVersion
{
    using System;
    using System.Collections.Generic;
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

        public static SemanticVersion NewestSemVerTag(this IRepository repository, Commit commit)
        {
            foreach (var tag in repository.TagsByDate(commit))
            {
                SemanticVersion version;
                if (SemanticVersion.TryParse(tag.Name, out version))
                {
                    return version;
                }
            }

            return null;
        }

        public static IEnumerable<Tag> SemVerTagsRelatedToVersion(this IRepository repository, SemanticVersion version)
        {
            foreach (var tag in repository.Tags)
            {
                SemanticVersion tagVersion = null;
                if (SemanticVersion.TryParse(tag.Name, out tagVersion))
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
    }
}