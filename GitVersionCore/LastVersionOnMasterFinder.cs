using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace GitVersion
{
    using System;

    public class LastVersionOnMasterFinder
    {
        public static DateTimeOffset Execute(IRepository repo, Commit commit)
        {
            //dont need an if for release since we want the current date for a release branch
            if (
                repo.Head.IsMaster() ||
                repo.Head.IsHotfix()
                )
            {
                var vp = FindLatestStableTaggedCommitReachableFrom(repo, commit);
                return vp.When();
            }
            return commit.When();
        }

        static Commit FindLatestStableTaggedCommitReachableFrom(IRepository repo, Commit commit)
        {
            var masterTip = repo.FindBranch("master").Tip;
            var ancestor = repo.Commits.FindMergeBase(masterTip, commit);

            var allTags = repo.Tags.ToList();

            foreach (var c in repo.Commits.QueryBy(new CommitFilter
            {
                Since = ancestor.Id,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            }
                ))
            {
                var vp = RetrieveStableVersionPointFor(allTags, c);

                if (vp != null)
                {
                    return vp;
                }
            }

            return null;
        }

        static Commit RetrieveStableVersionPointFor(IEnumerable<Tag> allTags, Commit c)
        {
            var tags = allTags
                .Where(tag => tag.PeeledTarget() == c)
                .Where(tag => IsStableRelease(tag.Name))
                .ToList();

            if (tags.Count == 0)
            {
                return null;
            }

            if (tags.Count > 1)
            {
                var message = string.Format("Commit '{0}' bears more than one stable tag: {1}", c.Id.ToString(7), string.Join(", ", tags.Select(t => t.Name)));
                throw new WarningException(message);
            }

            var stableTag = tags.Single();
            var commit = RetrieveMergeCommit(stableTag);

            return commit;
        }

        static bool IsStableRelease(string tagName)
        {
            ShortVersion shortVersion;
            return ShortVersionParser.TryParseMajorMinor(tagName, out shortVersion);
        }

        static Commit RetrieveMergeCommit(Tag stableTag)
        {
            var target = stableTag.PeeledTarget();
            var commit = target as Commit;
            if (commit != null)
            {
                return commit;
            }
            var message = string.Format("Target '{0}' of Tag '{1}' isn't a Commit.", target.Id.ToString(7), stableTag.Name);
            throw new WarningException(message);
        }


    }
}