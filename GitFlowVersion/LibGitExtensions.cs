namespace GitFlowVersion
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

        public static string Prefix(this ObjectId objectId)
        {
            return objectId.Sha.Substring(0, 8);
        }
        public static string Prefix(this Commit commit)
        {
            return commit.Sha.Substring(0, 8);
        }

        public static SemanticVersion NewestSemVerTag(this IRepository repository, Commit commit)
        {
            foreach (var tag in repository.TagsByDate(commit))
            {
                SemanticVersion version;
                if (SemanticVersionParser.TryParse(tag.Name, out version))
                {
                    return version;
                }
            }
            return null;
        }

        public static IEnumerable<Tag> TagsByDate(this IRepository repository, Commit commit)
        {
            return repository.Tags
                .Where(tag => tag.Target == commit)
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


        public static IEnumerable<Commit> CommitsPriorToThan(this Branch branch, DateTimeOffset olderThan)
        {
            return branch.Commits.SkipWhile(c => c.When() > olderThan);
        }
    }
}
