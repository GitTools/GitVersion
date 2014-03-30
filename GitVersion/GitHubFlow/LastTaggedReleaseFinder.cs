namespace GitVersion
{
    using System;
    using System.IO;
    using System.Linq;
    using LibGit2Sharp;

    public class LastTaggedReleaseFinder
    {
        string workingDirectory;
        Lazy<VersionTaggedCommit> lastTaggedRelease;

        public LastTaggedReleaseFinder(IRepository gitRepo, string workingDirectory)
        {
            this.workingDirectory = workingDirectory;
            lastTaggedRelease = new Lazy<VersionTaggedCommit>(() => GetVersion(gitRepo));
        }

        public VersionTaggedCommit GetVersion()
        {
            return lastTaggedRelease.Value;
        }

        private VersionTaggedCommit GetVersion(IRepository gitRepo)
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

            // Create a next version txt as 0.1.0
            var filePath = Path.Combine(workingDirectory, "NextVersion.txt");
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, "0.1.0");

            var commit = branch.Commits.Last();
            return new VersionTaggedCommit(commit, new SemanticVersion());
        }
    }
}