namespace GitVersion
{
    using System;
    using System.Linq;

    class OtherBranchVersionFinder 
    {
        public bool FindVersion(GitVersionContext context, SemanticVersion defaultNextVersion, out SemanticVersion semanticVersion)
        {
            var versionInBranch = GetVersionInBranch(context);
            if (versionInBranch == null)
            {
                if (!context.CurrentBranch.IsMaster())
                    defaultNextVersion.PreReleaseTag = context.CurrentBranch.Name.Replace("-", ".").Replace("/", ".");
                semanticVersion = null;

                return false;
            }

            var applicableTagsInDescendingOrder = context.Repository.SemVerTagsRelatedToVersion(context.Configuration, versionInBranch.Item2).OrderByDescending(tag => SemanticVersion.Parse(tag.Name, context.Configuration.TagPrefix)).ToList();
            var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsSinceLastTagOrBranchPoint(context, applicableTagsInDescendingOrder, BranchType.Unknown, "master");
            var semanticVersionPreReleaseTag = RecentTagVersionExtractor.RetrieveMostRecentOptionalTagVersion(context, applicableTagsInDescendingOrder) ?? CreateDefaultPreReleaseTag(context, versionInBranch.Item1);


            if (semanticVersionPreReleaseTag.Name == "release")
            {
                semanticVersionPreReleaseTag.Name = context.Configuration.ReleaseBranchTag;
            }

            semanticVersion = new SemanticVersion
            {
                Major = versionInBranch.Item2.Major,
                Minor = versionInBranch.Item2.Minor,
                Patch = versionInBranch.Item2.Patch,
                PreReleaseTag = semanticVersionPreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(nbHotfixCommits, context.CurrentBranch.Name, context.CurrentCommit.Sha, context.CurrentCommit.When())
            };
            return true;
        }

        Tuple<string, SemanticVersion> GetVersionInBranch(GitVersionContext context)
        {
            var branchParts = context.CurrentBranch.Name.Split('/', '-');
            foreach (var part in branchParts)
            {
                SemanticVersion semanticVersion;
                if (SemanticVersion.TryParse(part, context.Configuration.TagPrefix, out semanticVersion))
                {
                    return Tuple.Create(part, semanticVersion);
                }
            }

            return null;
        }

        SemanticVersionPreReleaseTag CreateDefaultPreReleaseTag(GitVersionContext context, string versionString)
        {
            return context.CurrentBranch.Name
                .Replace("/" + versionString, string.Empty)
                .Replace("-" + versionString, string.Empty) + ".1";
        }

    }
}