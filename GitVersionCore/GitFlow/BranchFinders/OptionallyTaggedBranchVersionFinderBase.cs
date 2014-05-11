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
            var nbHotfixCommits = NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, branchType, baseBranchName);

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

            var tagVersion = RetrieveMostRecentOptionalTagVersion(context.Repository, version, context.CurrentBranch.Commits.Take(nbHotfixCommits + 1));

            var sha = context.CurrentBranch.Tip.Sha;
            var releaseDate = ReleaseDateFinder.Execute(context.Repository, sha, version.Patch);
            var semanticVersion = new SemanticVersion
            {
                Major = version.Major,
                Minor = version.Minor,
                Patch = version.Patch,
                PreReleaseTag = version.PreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(
                    nbHotfixCommits, context.CurrentBranch.Name, releaseDate)
            };

            if (tagVersion != null)
            {
                tagVersion.PreReleaseTag.Number++;
                semanticVersion.PreReleaseTag = tagVersion.PreReleaseTag;
            }

            return semanticVersion;
        }

        bool IsMostRecentCommitTagged(GitVersionContext context, out SemanticVersion version)
        {
            var currentCommit = context.CurrentBranch.Commits.First();

            var tags = context.Repository.Tags
                .Where(tag => tag.PeeledTarget() == currentCommit)
                .ToList();

            foreach (var tag in tags)
            {
                if (SemanticVersion.TryParse(tag.Name, out version))
                {
                    return true;
                }
            }

            version = null;
            return false;
        }

        SemanticVersion RetrieveMostRecentOptionalTagVersion(
            IRepository repository, SemanticVersion branchVersion, IEnumerable<Commit> take)
        {
            foreach (var commit in take)
            {
                foreach (var tag in repository.TagsByDate(commit))
                {
                    SemanticVersion version;
                    if (!SemanticVersion.TryParse(tag.Name, out version))
                    {
                        continue;
                    }

                    if (branchVersion.Major != version.Major ||
                        branchVersion.Minor != version.Minor ||
                        branchVersion.Patch != version.Patch)
                    {
                        continue;
                    }

                    return version;
                }
            }

            return null;
        }

        void EnsureVersionIsValid(SemanticVersion version, Branch branch, BranchType branchType)
        {
            var msg = string.Format("Branch '{0}' doesn't respect the {1} branch naming convention. ",
                branch.Name, branchType);

            if (version.PreReleaseTag.HasTag())
            {
                throw new ErrorException(msg + string.Format("Supported format is '{0}-Major.Minor.Patch'.", branchType.ToString().ToLowerInvariant()));
            }

            switch (branchType)
            {
                case BranchType.Hotfix:
                    if (version.Patch == 0)
                    {
                        throw new ErrorException(msg + "A patch segment different than zero is required.");
                    }

                    break;

                case BranchType.Release:
                    if (version.Patch != 0)
                    {
                        throw new ErrorException(msg + "A patch segment equals to zero is required.");
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
                throw new ErrorException(message);
            }

            var filter = new CommitFilter
                         {
                             Since = branch.Tip,
                             Until = ancestor

                         };

            return repo.Commits.QueryBy(filter).Count();
        }
    }
}
