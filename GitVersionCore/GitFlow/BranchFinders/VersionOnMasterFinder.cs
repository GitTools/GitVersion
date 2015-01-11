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
                    SemanticVersion semanticVersion;
                    if (SemanticVersion.TryParse(tag.Name, context.Configuration.TagPrefix, out semanticVersion))
                    {
                        return new VersionPoint
                        {
                            Major = semanticVersion.Major,
                            Minor = semanticVersion.Minor,
                        };
                    }
                }

                SemanticVersion semanticVersionFromMergeMessage;
                if (MergeMessageParser.TryParse(commit, context.Configuration, out semanticVersionFromMergeMessage))
                {
                    return new VersionPoint
                    {
                        Major = semanticVersionFromMergeMessage.Major,
                        Minor = semanticVersionFromMergeMessage.Minor,
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
