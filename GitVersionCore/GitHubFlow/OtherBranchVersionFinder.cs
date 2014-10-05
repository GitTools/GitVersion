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
            var shortVersion = ShortVersionParser.Parse(versionString);

            SemanticVersionPreReleaseTag semanticVersionPreReleaseTag = context.CurrentBranch.Name.Replace("-" + versionString, string.Empty) + ".1";

            var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, BranchType.Unknown, "master");

            var tagVersion = RecentTagVersionExtractor.RetrieveMostRecentOptionalTagVersion(context.Repository, shortVersion, context.CurrentBranch.Commits.Take(nbHotfixCommits + 1));
            if (tagVersion != null)
            {
                semanticVersionPreReleaseTag = tagVersion;
            }

            if (semanticVersionPreReleaseTag.Name == "release")
            {
                semanticVersionPreReleaseTag.Name = "beta";
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

        static string GetUnknownBranchSuffix(Branch branch)
        {
            var unknownBranchSuffix = branch.Name.Split('-', '/');
            if (unknownBranchSuffix.Length == 1)
                return branch.Name;
            return unknownBranchSuffix[1];
        }

    }
}