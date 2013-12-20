namespace GitFlowVersion
{
    using System;

    class VersionOnMasterFinder
    {
        public VersionPoint Execute(GitFlowVersionContext context, DateTimeOffset olderThan)
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