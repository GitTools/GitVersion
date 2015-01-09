namespace GitVersion.VersioningModes
{
    using System.Collections.Generic;

    using LibGit2Sharp;

    public class ContinuousDeploymentMode : VersioningModeBase
    {
        public override SemanticVersionPreReleaseTag GetPreReleaseTag(GitVersionContext context, List<Tag> possibleTags, int numberOfCommits)
        {
            return context.Configuration.Tag + "." + numberOfCommits;
        }
    }
}