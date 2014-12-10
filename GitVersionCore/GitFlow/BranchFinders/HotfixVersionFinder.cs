namespace GitVersion
{
    using System.Linq;

    using LibGit2Sharp;

    class HotfixVersionFinder 
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var versionString = GetSuffix(context.CurrentBranch);
            var shortVersion = SemanticVersion.Parse(versionString, context.Configuration.TagPrefix);

            EnsureVersionIsValid(shortVersion, context.CurrentBranch);
            
            var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, BranchType.Hotfix, "master");
            var tagsInDescendingOrder = context.Repository.SemVerTagsRelatedToVersion(context.Configuration, shortVersion).OrderByDescending(tag => SemanticVersion.Parse(tag.Name, context.Configuration.TagPrefix)).ToList();
            var semanticVersionPreReleaseTag = context.CurrentBranchConfig.VersioningMode.GetInstance().GetPreReleaseTag(context, tagsInDescendingOrder, nbHotfixCommits);
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                PreReleaseTag = semanticVersionPreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(nbHotfixCommits, context.CurrentBranch.Name, context.CurrentCommit.Sha, context.CurrentCommit.When())
            };
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
