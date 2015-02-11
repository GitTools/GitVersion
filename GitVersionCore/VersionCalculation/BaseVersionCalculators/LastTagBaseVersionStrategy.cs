namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System.Linq;
    using LibGit2Sharp;

    public class LastTagBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            VersionTaggedCommit version;
            if (GetVersion(context, out version))
            {
                var shouldUpdateVersion = version.Commit.Sha != context.CurrentCommit.Sha;
                return new BaseVersion(string.Format("Git tag '{0}'", version.Tag), shouldUpdateVersion, shouldUpdateVersion, version.SemVer, version.Commit, null);
            }

            return null;
        }

        bool GetVersion(GitVersionContext context, out VersionTaggedCommit versionTaggedCommit)
        {
            var tags = context.Repository.Tags.Select(t =>
            {
                SemanticVersion version;
                if (SemanticVersion.TryParse(t.Name, context.Configuration.GitTagPrefix, out version))
                {
                    return new VersionTaggedCommit((Commit)t.Target, version, t.Name);
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

        class VersionTaggedCommit
        {
            public string Tag;
            public Commit Commit;
            public SemanticVersion SemVer;

            public VersionTaggedCommit(Commit commit, SemanticVersion semVer, string tag)
            {
                Tag = tag;
                Commit = commit;
                SemVer = semVer;
            }
        }
    }
}