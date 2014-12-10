namespace GitVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class OtherBranchVersionFinder 
    {
        public bool FindVersion(GitVersionContext context, out SemanticVersion semanticVersion)
        {
            var versionString = GetUnknownBranchSuffix(context.CurrentBranch);
            if (!versionString.Contains("."))
            {
                semanticVersion = null;
                return false;
            }
            var shortVersion = SemanticVersion.Parse(versionString, context.Configuration.TagPrefix);


            var applicableTagsInDescendingOrder = context.Repository.SemVerTagsRelatedToVersion(context.Configuration, shortVersion).OrderByDescending(tag => SemanticVersion.Parse(tag.Name, context.Configuration.TagPrefix)).ToList();
            var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsSinceLastTagOrBranchPoint(context, applicableTagsInDescendingOrder, BranchType.Unknown, "master");
            var semanticVersionPreReleaseTag = RecentTagVersionExtractor.RetrieveMostRecentOptionalTagVersion(context, applicableTagsInDescendingOrder) ?? CreateDefaultPreReleaseTag(context, versionString);


            if (semanticVersionPreReleaseTag.Name == "release")
            {
                semanticVersionPreReleaseTag.Name = context.Configuration.Branches["release[/-]"].Tag;
            }

            semanticVersion = new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                PreReleaseTag = semanticVersionPreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(nbHotfixCommits, context.CurrentBranch.Name, context.CurrentCommit.Sha, context.CurrentCommit.When())
            };
            return true;
        }

        SemanticVersionPreReleaseTag CreateDefaultPreReleaseTag(GitVersionContext context, string versionString)
        {
            return context.CurrentBranch.Name
                .Replace("-" + versionString, string.Empty)
                .Replace("/" + versionString, string.Empty) + ".1";
        }

        static string GetUnknownBranchSuffix(Branch branch)
        {
            var unknownBranchSuffix = branch.Name.Split('-', '/');
            if (unknownBranchSuffix.Length == 1)
                return branch.Name;
            return unknownBranchSuffix[1];
        }

    }
}