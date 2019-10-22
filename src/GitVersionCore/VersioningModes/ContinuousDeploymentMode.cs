using System.Collections.Generic;
using GitVersion.SemanticVersioning;
using LibGit2Sharp;

namespace GitVersion.VersioningModes
{
    public class ContinuousDeploymentMode : VersioningModeBase
    {
        public override SemanticVersionPreReleaseTag GetPreReleaseTag(GitVersionContext context, List<Tag> possibleTags, int numberOfCommits)
        {
            return context.Configuration.Tag + "." + numberOfCommits;
        }
    }
}