namespace GitVersion
{
    using System.Linq;
    using LibGit2Sharp;

    public class LastTaggedReleaseFinder
    {
        GitVersionContext context;

        public LastTaggedReleaseFinder(GitVersionContext context)
        {
            this.context = context;
        }

        public bool GetVersion(out VersionTaggedCommit versionTaggedCommit)
        {
            var tags = context.Repository.Tags.Select(t =>
            {
                SemanticVersion version;
                if (SemanticVersion.TryParse(t.Name, context.Configuration.TagPrefix, out version))
                {
                    return new VersionTaggedCommit((Commit)t.Target, version);
                }
                return null;
            })
                .Where(a => a != null)
                .ToArray();
            var olderThan = context.CurrentCommit.When();
            var lastTaggedCommit =
                context.CurrentBranch.Commits.FirstOrDefault(c => c.When() <= olderThan && tags.Any(a => a.Commit == c));

            if (lastTaggedCommit != null)
            {
                versionTaggedCommit = tags.Last(a => a.Commit.Sha == lastTaggedCommit.Sha);
                return true;
            }

            versionTaggedCommit = null;
            return false;
        }
    }
}