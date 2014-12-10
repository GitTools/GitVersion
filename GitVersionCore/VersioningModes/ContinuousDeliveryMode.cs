namespace GitVersion.VersioningModes
{
    using System.Collections.Generic;

    using LibGit2Sharp;

    public class ContinuousDeliveryMode : VersioningModeBase
    {
        public override SemanticVersionPreReleaseTag GetPreReleaseTag(GitVersionContext context, List<Tag> possibleCommits, int numberOfCommits)
        {
            return RecentTagVersionExtractor.RetrieveMostRecentOptionalTagVersion(context, possibleCommits) 
                ?? context.CurrentBranchConfig.Tag + ".1";
        }
    }
}
