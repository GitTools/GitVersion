namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class VersionOnMasterFinder
    {
        public IRepository Repository;
        public DateTimeOffset OlderThan;

        public VersionPoint Execute()
        {
            var masterBranch = Repository
                .GetBranch("master");
            foreach (var commit in masterBranch.CommitsPriorToThan(OlderThan))
            {
                foreach (var tag in Repository.Tags.Where(tag => tag.Target == commit).Reverse())
                {
                    int major;
                    int minor;
                    if (ShortVersionParser.TryParseMajorMinor(tag.Name, out major, out minor))
                    {
                        return new VersionPoint
                        {
                            Major = major,
                            Minor = minor,
                            Timestamp = commit.When()
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
                            Timestamp = commit.When()
                        };
                    }
                }

            }
            return new VersionPoint
            {
                Major = 0,
                Minor = 1,
                Timestamp = DateTimeOffset.MinValue
            };
        }

    }
}