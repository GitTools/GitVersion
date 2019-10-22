using System.Collections.Generic;
using GitVersion.SemanticVersioning;
using LibGit2Sharp;

namespace GitVersion.VersioningModes
{
    public abstract class VersioningModeBase
    {
        public abstract SemanticVersionPreReleaseTag GetPreReleaseTag(GitVersionContext context, List<Tag> possibleTags, int numberOfCommits);
    }
}
