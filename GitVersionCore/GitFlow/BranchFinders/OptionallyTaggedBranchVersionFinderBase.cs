namespace GitVersion
{
    using System;
    using System.Collections.Generic;
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
            var version = SemanticVersion.Parse(versionString);

            EnsureVersionIsValid(version, context.CurrentBranch, branchType);

            if (branchType == BranchType.Hotfix)
                version.PreReleaseTag = "beta.1";
            if (branchType == BranchType.Release)
                version.PreReleaseTag = "beta.1";
            if (branchType == BranchType.Unknown)
                version.PreReleaseTag = context.CurrentBranch.Name.Replace("-" + versionString, string.Empty) + ".1";

            var tagsApplicableToBranchVersion = context.Repository.SemVerTagsRelatedToVersion(version).OrderByDescending(tag => SemanticVersion.Parse(tag.Name)).ToList();
            var latestTaggedCommit = tagsApplicableToBranchVersion.Select(tag => tag.Target).FirstOrDefault();

            var numberOfCommitsSinceLastTagOrBranchPoint = NumberOfCommitsSinceLastTagOrBranchPoint(context, tagsApplicableToBranchVersion, branchType, baseBranchName);

            var tagVersion = RetrieveMostRecentOptionalTagVersion(tagsApplicableToBranchVersion);

            var sha = context.CurrentCommit.Sha;
            var releaseDate = ReleaseDateFinder.Execute(context.Repository, sha, version.Patch);
            var semanticVersion = new SemanticVersion
            {
                Major = version.Major,
                Minor = version.Minor,
                Patch = version.Patch,
                PreReleaseTag = version.PreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(
                    numberOfCommitsSinceLastTagOrBranchPoint, context.CurrentBranch.Name, releaseDate)
            };

            if (tagVersion != null)
            {
                if (latestTaggedCommit != context.CurrentCommit)
                {
                    tagVersion.PreReleaseTag.Number++;
                }
                semanticVersion.PreReleaseTag = tagVersion.PreReleaseTag;
            }

            return semanticVersion;

        }

        SemanticVersion RetrieveMostRecentOptionalTagVersion(IEnumerable<Tag> tagsApplicableToBranchVersion)
        {
            return tagsApplicableToBranchVersion.Select(tag => SemanticVersion.Parse(tag.Name)).OrderByDescending(version => version).FirstOrDefault();
        }


        int NumberOfCommitsSinceLastTagOrBranchPoint(GitVersionContext context, ICollection<Tag> tagsInDescVersionOrder, BranchType branchType, string baseBranchName)
        {
            if (!tagsInDescVersionOrder.Any())
            {
                return NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, branchType, baseBranchName);
            }
            
            var mostRecentTag = tagsInDescVersionOrder.First();
            var ancestor = mostRecentTag;
            if (mostRecentTag.Target == context.CurrentCommit)
            {
                var previousTag = tagsInDescVersionOrder.Skip(1).FirstOrDefault();
                if (previousTag != null)
                {
                    ancestor = previousTag;
                }
                else
                {
                    return NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, branchType, baseBranchName);
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

        void EnsureVersionIsValid(SemanticVersion version, Branch branch, BranchType branchType)
        {
            var msg = string.Format("Branch '{0}' doesn't respect the {1} branch naming convention. ",
                branch.Name, branchType);

            if (version.PreReleaseTag.HasTag())
            {
                throw new WarningException(msg + string.Format("Supported format is '{0}-Major.Minor.Patch'.", branchType.ToString().ToLowerInvariant()));
            }

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

        int NumberOfCommitsInBranchNotKnownFromBaseBranch(
            IRepository repo,
            Branch branch,
            BranchType branchType,
            string baseBranchName)
        {
            var baseTip = repo.FindBranch(baseBranchName).Tip;
            if (branch.Tip == baseTip)
            {
                // The branch bears no additional commit
                return 0;
            }

            var ancestor = repo.Commits.FindMergeBase(
                baseTip,
                branch.Tip);

            if (ancestor == null)
            {
                var message = string.Format("A {0} branch is expected to branch off of '{1}'. However, branch '{1}' and '{2}' do not share a common ancestor.", branchType, baseBranchName, branch.Name);
                throw new WarningException(message);
            }

            var filter = new CommitFilter
                         {
                             Since = branch.Tip,
                             Until = ancestor,
                             SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
                         };

            return repo.Commits.QueryBy(filter).Count();
        }
    }
}