namespace GitVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    public class LastTaggedReleaseFinder
    {
        Lazy<VersionTaggedCommit> lastTaggedRelease;

        public LastTaggedReleaseFinder(GitVersionContext context)
        {
            lastTaggedRelease = new Lazy<VersionTaggedCommit>(() => GetVersion(context));
        }

        public VersionTaggedCommit GetVersion()
        {
            return lastTaggedRelease.Value;
        }

        VersionTaggedCommit GetVersion(GitVersionContext context)
        {
            var tags = context.Repository.Tags.Select(t =>
            {
                SemanticVersion version;
                if (SemanticVersion.TryParse(t.Name.TrimStart('v'), out version))
                {
                    return new VersionTaggedCommit((Commit)t.Target, version);
                }
                return null;
            })
                .Where(a => a != null)
                .ToArray();
            var olderThan = context.CurrentBranch.Tip.Committer.When;
            var lastTaggedCommit =
                context.CurrentBranch.Commits.FirstOrDefault(c => c.Committer.When <= olderThan && tags.Any(a => a.Commit == c));

            if (lastTaggedCommit != null)
                return tags.Last(a => a.Commit.Sha == lastTaggedCommit.Sha);

            var commit = context.CurrentBranch.Commits.Last();
            return new VersionTaggedCommit(commit, new SemanticVersion());
        }
    }
}