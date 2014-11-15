namespace GitVersion
{
    using System.Linq;
    using System.Text.RegularExpressions;
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
                var match = Regex.Match(t.Name, string.Format("({0})?(?<version>.*)", context.Configuration.TagPrefix));
                SemanticVersion version;
                if (SemanticVersion.TryParse(match.Groups["version"].Value, out version))
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