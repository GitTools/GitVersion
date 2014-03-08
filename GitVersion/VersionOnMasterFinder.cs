namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using LibGit2Sharp;

    class VersionOnMasterFinder
    {
        public VersionPoint Execute(GitVersionContext context, DateTimeOffset olderThan)
        {
            var masterBranch = context.Repository.FindBranch("master");
            foreach (var commit in masterBranch.CommitsPriorToThan(olderThan))
            {
                foreach (var tag in context.Repository.TagsByDate(commit))
                {
                    int major;
                    int minor;
                    if (ShortVersionParser.TryParseMajorMinor(tag.Name, out major, out minor))
                    {
                        return new VersionPoint
                        {
                            Major = major,
                            Minor = minor,
                            Timestamp = commit.When(),
                            CommitSha = commit.Sha,
                        };
                    }
                }
                string versionString;
                if (MergeMessageParser.TryParse(commit, out versionString))
                {
                    int major;
                    int minor;
                    if (ShortVersionParser.TryParseMajorMinor(versionString, out major, out minor))
                    {
                        return new VersionPoint
                        {
                            Major = major,
                            Minor = minor,
                            Timestamp = commit.When(),
                            CommitSha = commit.Sha,
                        };
                    }
                }

            }
            return new VersionPoint
            {
                Major = 0,
                Minor = 1,
                Timestamp = DateTimeOffset.MinValue,
                CommitSha = null,
            };
        }

        public VersionPoint FindLatestStableTaggedCommitReachableFrom(IRepository repo, Commit commit)
        {
            var masterTip = repo.FindBranch("master").Tip;
            var ancestor = repo.Commits.FindCommonAncestor(masterTip, commit);

            var allTags = repo.Tags.ToList();

            foreach (var c in repo.Commits.QueryBy(new CommitFilter { Since = ancestor.Id }))
            {
                var vp = RetrieveStableVersionPointFor(allTags, c);

                if (vp != null)
                {
                    return vp;
                }
            }

            return null;
        }

        static VersionPoint RetrieveStableVersionPointFor(IEnumerable<Tag> allTags, Commit c)
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
                throw new ErrorException(
                    string.Format("Commit '{0}' bears more than one stable tag: {1}",
                        c.Id.ToString(7), string.Join(", ", tags.Select(t => t.Name))));
            }

            var stableTag = tags.Single();
            var commit = RetrieveMergeCommit(stableTag);

            return BuildFrom(stableTag, commit);
        }

        static VersionPoint BuildFrom(Tag stableTag, Commit commit)
        {
            int major;
            int minor;

            var hasParsed = ShortVersionParser.TryParseMajorMinor(stableTag.Name, out major, out minor);
            Debug.Assert(hasParsed);

            return new VersionPoint
                   {
                       Major = major,
                       Minor = minor,
                       CommitSha = commit.Id.Sha,
                   };
        }

        static Commit RetrieveMergeCommit(Tag stableTag)
        {
            var target = stableTag.PeeledTarget();
            if (!(target is Commit))
            {
                throw new ErrorException(
                    string.Format("Target '{0}' of Tag '{1}' isn't a Commit.",
                        target.Id.ToString(7), stableTag.Name));
            }

            var targetCommit = (Commit) target;

            return targetCommit;
        }

        static bool IsStableRelease(string tagName)
        {
            int major;
            int minor;

            return ShortVersionParser.TryParseMajorMinor(tagName, out major, out minor);
        }
    }
}
