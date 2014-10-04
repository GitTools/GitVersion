namespace GitVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    abstract class OptionallyTaggedBranchVersionFinderBase
    {
        protected SemanticVersion FindVersion(
            GitVersionContext context,
            BranchType branchType,
            string baseBranchName)
        {

            var versionString = context.CurrentBranch.GetSuffix(branchType);
            if (!versionString.Contains("."))
                return new SemanticVersion();
            var shortVersion = ShortVersionParser.Parse(versionString);

            EnsureVersionIsValid(shortVersion, context.CurrentBranch, branchType);
            var semanticVersionPreReleaseTag = new SemanticVersionPreReleaseTag();
            if (branchType == BranchType.Hotfix)
                semanticVersionPreReleaseTag = "beta.1";
            if (branchType == BranchType.Release)
                semanticVersionPreReleaseTag = "beta.1";
            if (branchType == BranchType.Unknown)
                semanticVersionPreReleaseTag = context.CurrentBranch.Name.Replace("-" + versionString, string.Empty) + ".1";


            var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, branchType, baseBranchName);
            
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



        void EnsureVersionIsValid(ShortVersion version, Branch branch, BranchType branchType)
        {
            var msg = string.Format("Branch '{0}' doesn't respect the {1} branch naming convention. ", branch.Name, branchType);
        
            switch (branchType)
            {
                case BranchType.Hotfix:
                    if (version.Patch == 0)
                    {
                        throw new WarningException(msg + "A patch segment different than zero is required.");
                    }

                    break;

                case BranchType.Release:
                    if (version.Patch != 0)
                    {
                        throw new WarningException(msg + "A patch segment equals to zero is required.");
                    }

                    break;

                case BranchType.Unknown:
                    break;

                default:
                    throw new NotSupportedException(string.Format("Unexpected branch type {0}.", branchType));
            }
        }

    }
}