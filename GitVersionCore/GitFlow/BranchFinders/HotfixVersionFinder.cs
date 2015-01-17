namespace GitVersion
{
    using LibGit2Sharp;

    class HotfixVersionFinder 
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var versionString = GetSuffix(context.CurrentBranch);
            var shortVersion = SemanticVersion.Parse(versionString, context.Configuration.TagPrefix);

            EnsureVersionIsValid(shortVersion, context.CurrentBranch);
            
            var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, BranchType.Hotfix, "master");

            var semanticVersionPreReleaseTag = GetSemanticVersionPreReleaseTag(context, shortVersion);
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                PreReleaseTag = semanticVersionPreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(nbHotfixCommits, context.CurrentBranch.Name, context.CurrentCommit.Sha, context.CurrentCommit.When())
            };
        }

        static string GetSemanticVersionPreReleaseTag(GitVersionContext context, SemanticVersion shortVersion)
        {
            var semanticVersionPreReleaseTag = context.Configuration.ReleaseBranchTag + ".1";
            var tagVersion = RecentTagVersionExtractor.RetrieveMostRecentOptionalTagVersion(context, shortVersion);
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

        void EnsureVersionIsValid(SemanticVersion version, Branch branch)
        {
            if (version.Patch == 0)
            {
                var message = string.Format("Branch '{0}' doesn't respect the Hotfix branch naming convention. A patch segment different than zero is required.", branch.Name);
                throw new WarningException(message);
            }
        }
    }
}
