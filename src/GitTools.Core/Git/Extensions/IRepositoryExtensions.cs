namespace GitTools.Git
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    public static class IRepositoryExtensions
    {
        private static readonly Dictionary<string, TaggedCommit> Cache = new Dictionary<string, TaggedCommit>();

        public static TaggedCommit GetLastTaggedCommit(this IRepository repository)
        {
            return GetTag(repository, string.Empty);
        }

        public static TaggedCommit GetFirstCommit(this IRepository repository)
        {
            var branch = repository.Head;
            return new TaggedCommit(branch.Commits.Last(), "Initial Commit");
        }

        public static TaggedCommit GetTag(this IRepository repository, string fromTag)
        {
            if (!Cache.ContainsKey(fromTag))
            {
                var lastTaggedCommit = GetLastTaggedCommit(repository, t => string.IsNullOrEmpty(fromTag) || t.TagName == fromTag);
                Cache.Add(fromTag, lastTaggedCommit);
            }

            return Cache[fromTag];
        }

        public static TaggedCommit GetLastTaggedCommit(this IRepository repository, Func<TaggedCommit, bool> filterTags)
        {
            var branch = repository.Head;
            var tags = repository.Tags
                              .Select(t => new TaggedCommit((Commit) t.Target, t.FriendlyName))
                              .Where(filterTags)
                              .ToArray();
            var olderThan = branch.Tip.Author.When;
            var lastTaggedCommit = branch.Commits.FirstOrDefault(c => c.Author.When <= olderThan && tags.Any(a => a.Commit == c));
            if (lastTaggedCommit != null)
            {
                return tags.FirstOrDefault(a => a.Commit.Sha == lastTaggedCommit.Sha);
            }

            return new TaggedCommit(branch.Commits.Last(), "Initial Commit");
        }
    }
}