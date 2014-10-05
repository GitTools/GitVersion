namespace GitVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class HotfixVersionFinder 
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var versionString = GetSuffix(context.CurrentBranch);
            var shortVersion = ShortVersionParser.Parse(versionString);

            EnsureVersionIsValid(shortVersion, context.CurrentBranch);
            
            var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, BranchType.Hotfix, "master");

            var semanticVersionPreReleaseTag = GetSemanticVersionPreReleaseTag(context, shortVersion, nbHotfixCommits);
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                PreReleaseTag = semanticVersionPreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(nbHotfixCommits, context.CurrentBranch.Name, context.CurrentCommit.Sha, context.CurrentCommit.When())
            };
        }

        static string GetSemanticVersionPreReleaseTag(GitVersionContext context, ShortVersion shortVersion, int nbHotfixCommits)
        {
            var semanticVersionPreReleaseTag = "beta.1";
            var tagVersion = RecentTagVersionExtractor.RetrieveMostRecentOptionalTagVersion(context.Repository, shortVersion, context.CurrentBranch.Commits.Take(nbHotfixCommits + 1));
            if (tagVersion != null)
            {
                semanticVersionPreReleaseTag = tagVersion;
            }
            return semanticVersionPreReleaseTag;
        }

        static string GetSuffix(Branch branch)
        {
            return branch.Name.TrimStart("hotfix-").TrimStart("hotfix/");
        }
        void EnsureVersionIsValid(ShortVersion version, Branch branch)
        {
            if (version.Patch == 0)
            {
                var message = string.Format("Branch '{0}' doesn't respect the Hotfix branch naming convention. A patch segment different than zero is required.", branch.Name);
                throw new WarningException(message);
            }
        }
    }
}
