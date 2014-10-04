namespace GitVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class ReleaseVersionFinder 
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var versionString = GetSuffix(context.CurrentBranch);
            if (!versionString.Contains("."))
                return new SemanticVersion();
            var shortVersion = ShortVersionParser.Parse(versionString);

            EnsureVersionIsValid(shortVersion, context.CurrentBranch);
            var semanticVersionPreReleaseTag = "beta.1";

            var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, BranchType.Release, "develop");
            
            var tagVersion = RecentTagVersionExtractor.RetrieveMostRecentOptionalTagVersion(context.Repository, shortVersion, context.CurrentBranch.Commits.Take(nbHotfixCommits + 1));
            if (tagVersion != null)
            {
                semanticVersionPreReleaseTag = tagVersion;
            }
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                PreReleaseTag = semanticVersionPreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(nbHotfixCommits, context.CurrentBranch.Name, context.CurrentCommit.Sha, context.CurrentCommit.When())
            };
        }

        void EnsureVersionIsValid(ShortVersion version, Branch branch)
        {
            if (version.Patch != 0)
            {
                var message = string.Format("Branch '{0}' doesn't respect the Release branch naming convention. A patch segment equals to zero is required.", branch.Name);
                throw new WarningException(message);
            }

        }

        static string GetSuffix(Branch branch)
        {
            return branch.Name.TrimStart("release-").TrimStart("release/");
        }
    }
}
