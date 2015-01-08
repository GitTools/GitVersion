namespace GitVersion
{
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    static class BranchCommitDifferenceFinder
    {
        public static int NumberOfCommitsInBranchNotKnownFromBaseBranch(IRepository repo, Branch branch, string baseBranchName)
        {
            return NumberOfCommitsInBranchNotKnownFromBaseBranch(repo, branch, BranchType.Unknown, baseBranchName);
        }

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

        public static int NumberOfCommitsSinceLastTagOrBranchPoint(GitVersionContext context, List<Tag> tagsInDescendingOrder, string baseBranchName)
        {
           return NumberOfCommitsSinceLastTagOrBranchPoint(context, tagsInDescendingOrder, BranchType.Unknown, baseBranchName);
        }

        public static int NumberOfCommitsSinceLastTagOrBranchPoint(GitVersionContext context, List<Tag> tagsInDescendingOrder, BranchType branchType,  string baseBranchName)
        {
            if (!tagsInDescendingOrder.Any())
            {
                return NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, branchType, baseBranchName);
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
                    return NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, BranchType.Release, baseBranchName);
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
    }
}