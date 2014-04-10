namespace GitVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    public class LastTaggedReleaseFinder
    {
        Lazy<VersionTaggedCommit> lastTaggedRelease;

        public LastTaggedReleaseFinder(IRepository gitRepo)
        {
            lastTaggedRelease = new Lazy<VersionTaggedCommit>(() => GetVersion(gitRepo));
        }

        public VersionTaggedCommit GetVersion()
        {
            return lastTaggedRelease.Value;
        }

        VersionTaggedCommit GetVersion(IRepository gitRepo)
        {
            var branch = gitRepo.FindBranch("master");
            var tags = gitRepo.Tags.Select(t =>
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
            var olderThan = branch.Tip.Committer.When;
            var lastTaggedCommit =
                branch.Commits.FirstOrDefault(c => c.Committer.When <= olderThan && tags.Any(a => a.Commit == c));

            if (lastTaggedCommit != null)
                return tags.Last(a => a.Commit.Sha == lastTaggedCommit.Sha);

            var commit = branch.Commits.Last();
            return new VersionTaggedCommit(commit, new SemanticVersion());
        }
    }
}