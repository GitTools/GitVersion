namespace GitVersion
{
    using System;

    class VersionOnMasterFinder
    {
        public VersionPoint Execute(GitVersionContext context, DateTimeOffset olderThan)
        {
            var masterBranch = context.Repository.FindBranch("master");
            foreach (var commit in masterBranch.CommitsPriorToThan(olderThan))
            {
                foreach (var tag in context.Repository.TagsByDate(commit))
                {
                    ShortVersion shortVersion;
                    if (ShortVersionParser.TryParseMajorMinor(tag.Name, out shortVersion))
                    {
                        return new VersionPoint
                        {
                            Major = shortVersion.Major,
                            Minor = shortVersion.Minor,
                        };
                    }
                }

                ShortVersion shortVersionFromMergeMessage;
                if (MergeMessageParser.TryParse(commit, out shortVersionFromMergeMessage))
                {
                    return new VersionPoint
                    {
                        Major = shortVersionFromMergeMessage.Major,
                        Minor = shortVersionFromMergeMessage.Minor,
                    };
                }
            }
            return new VersionPoint
            {
                Major = 0,
                Minor = 1,
            };
        }

    



    }
}
