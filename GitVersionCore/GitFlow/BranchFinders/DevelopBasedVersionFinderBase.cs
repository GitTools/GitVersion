namespace GitVersion
{
    using System.Linq;
    using LibGit2Sharp;

    abstract class DevelopBasedVersionFinderBase
    {
        protected SemanticVersion FindVersion(
            GitVersionContext context,
            BranchType branchType)
        {
            var ancestor = FindCommonAncestorWithDevelop(context.Repository, context.CurrentBranch, branchType);

            if (!IsThereAnyCommitOnTheBranch(context.Repository, context.CurrentBranch))
            {
                var developVersionFinder = new DevelopVersionFinder();
                return developVersionFinder.FindVersion(context);
            }

            var versionOnMasterFinder = new VersionOnMasterFinder();
            var versionFromMaster = versionOnMasterFinder.Execute(context, context.CurrentCommit.Committer.When);

            var numberOfCommitsOnBranchSinceCommit = NumberOfCommitsOnBranchSinceCommit(context, ancestor);
            var releaseDate = ReleaseDateFinder.Execute(context.Repository, context.CurrentCommit, 0);
            var preReleaseTag = context.CurrentBranch.Name
                .TrimStart(branchType.ToString() + '-')
                .TrimStart(branchType.ToString() + '/');
            var semanticVersion = new SemanticVersion
            {
                Major = versionFromMaster.Major,
                Minor = versionFromMaster.Minor + 1,
                Patch = 0,
                PreReleaseTag = preReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(
                    numberOfCommitsOnBranchSinceCommit,
                    context.CurrentBranch.Name, releaseDate)
            };

            semanticVersion.OverrideVersionManuallyIfNeeded(context.Repository);

            return semanticVersion;
        }

        int NumberOfCommitsOnBranchSinceCommit(GitVersionContext context, Commit commit)
        {
            var qf = new CommitFilter
            {
                Since = context.CurrentBranch,
                Until = commit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            return context.Repository.Commits
                .QueryBy(qf)
                .Count();
        }

        Commit FindCommonAncestorWithDevelop(IRepository repo, Branch branch, BranchType branchType)
        {
            var ancestor = repo.Commits.FindMergeBase(
                repo.FindBranch("develop").Tip,
                branch.Tip);

            if (ancestor != null)
            {
                return ancestor;
            }

            throw new WarningException(
                string.Format("A {0} branch is expected to branch off of 'develop'. "
                              + "However, branch 'develop' and '{1}' do not share a common ancestor."
                    , branchType, branch.Name));
        }

        public bool IsThereAnyCommitOnTheBranch(IRepository repo, Branch branch)
        {
            var filter = new CommitFilter
            {
                Since = branch,
                Until = repo.FindBranch("develop")
            };

            var commits = repo.Commits.QueryBy(filter);

            if (!commits.Any())
            {
                return false;
            }

            return true;
        }
    }
}