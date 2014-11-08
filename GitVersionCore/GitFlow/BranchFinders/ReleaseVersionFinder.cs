namespace GitVersion
{
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    class ReleaseVersionFinder 
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var versionString = GetSuffix(context.CurrentBranch);
            var shortVersion = ShortVersionParser.Parse(versionString);

            EnsureVersionIsValid(shortVersion, context.CurrentBranch);
            SemanticVersionPreReleaseTag semanticVersionPreReleaseTag = "beta.1";
            
            var tagsApplicableToBranchVersion = context.Repository.SemVerTagsRelatedToVersion(shortVersion).OrderByDescending(tag => SemanticVersion.Parse(tag.Name)).ToList();
            var latestTaggedCommit = tagsApplicableToBranchVersion.Select(tag => tag.Target).FirstOrDefault();

            var numberOfCommitsSinceLastTagOrBranchPoint = NumberOfCommitsSinceLastTagOrBranchPoint(context, tagsApplicableToBranchVersion, "develop");

            var tagVersion = RetrieveMostRecentOptionalTagVersion(tagsApplicableToBranchVersion);

            if (tagVersion != null)
            {
                semanticVersionPreReleaseTag = tagVersion.PreReleaseTag;
                if (latestTaggedCommit != context.CurrentCommit)
                {
                    semanticVersionPreReleaseTag.Number++;
                }
            }

            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                PreReleaseTag = semanticVersionPreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(numberOfCommitsSinceLastTagOrBranchPoint, context.CurrentBranch.Name, context.CurrentCommit.Sha, context.CurrentCommit.When())
            };
        }

        static SemanticVersion RetrieveMostRecentOptionalTagVersion(List<Tag> tagsInDescendingOrder)
        {
            return tagsInDescendingOrder.Select(tag => SemanticVersion.Parse(tag.Name)).FirstOrDefault();
        }

        static int NumberOfCommitsSinceLastTagOrBranchPoint(GitVersionContext context, List<Tag> tagsInDescendingOrder,  string baseBranchName)
        {
            if (!tagsInDescendingOrder.Any())
            {
                return BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, BranchType.Release, baseBranchName);
            }

            var mostRecentTag = tagsInDescendingOrder.First();
            var ancestor = mostRecentTag;
            if (mostRecentTag.Target == context.CurrentCommit)
            {
                var previousTag = tagsInDescendingOrder.Skip(1).FirstOrDefault();
                if (previousTag != null)
                {
                    ancestor = previousTag;
                }
                else
                {
                    return BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, BranchType.Release, baseBranchName);
                }

            }

            var filter = new CommitFilter
            {
                Since = context.CurrentCommit,
                Until = ancestor.Target,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            return context.Repository.Commits.QueryBy(filter).Count() - 1;
        }

        static void EnsureVersionIsValid(ShortVersion version, Branch branch)
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
