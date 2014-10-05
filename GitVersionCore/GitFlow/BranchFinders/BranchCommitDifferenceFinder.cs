namespace GitVersion
{
    using System.Linq;
    using LibGit2Sharp;

    static class BranchCommitDifferenceFinder
    {
        public static int NumberOfCommitsInBranchNotKnownFromBaseBranch(IRepository repo, Branch branch, BranchType branchType, string baseBranchName)
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

            return repo.Commits.QueryBy(filter)
                .Count();
        }
    }
}