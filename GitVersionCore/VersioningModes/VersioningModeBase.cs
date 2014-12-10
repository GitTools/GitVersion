namespace GitVersion.VersioningModes
{
    using System.Collections.Generic;

    using LibGit2Sharp;

    public abstract class VersioningModeBase
    {
        public abstract SemanticVersionPreReleaseTag GetPreReleaseTag(GitVersionContext context, List<Tag> possibleTags, int numberOfCommits);
    }
}
