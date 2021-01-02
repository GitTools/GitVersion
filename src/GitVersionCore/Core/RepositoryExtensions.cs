using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

namespace GitVersion
{
    public static class RepositoryExtensions
    {
        public static bool GetMatchingCommitBranch(this IGitRepository repository, Commit baseVersionSource, Branch branch, Commit firstMatchingCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = (LibGit2Sharp.Branch)branch,
                ExcludeReachableFrom = baseVersionSource,
                FirstParentOnly = true,
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            return commitCollection.Contains(firstMatchingCommit);
        }

        public static IEnumerable<Commit> GetCommitsReacheableFrom(this IGitRepository repository, Commit commit, Branch branch)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = (LibGit2Sharp.Branch)branch
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            return commitCollection.Where(c => c.Sha == commit.Sha);
        }

        public static List<Commit> GetCommitsReacheableFromHead(this IGitRepository repository, Commit headCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = headCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
            };

            var commitCollection = repository.Commits.QueryBy(filter);

            return commitCollection.ToList();
        }

        public static Commit GetForwardMerge(this IGitRepository repository, Commit commitToFindCommonBase, Commit findMergeBase)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = commitToFindCommonBase,
                ExcludeReachableFrom = findMergeBase
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            var forwardMerge = commitCollection
                .FirstOrDefault(c => c.Parents.Contains(findMergeBase));
            return forwardMerge;
        }

        public static IEnumerable<Commit> GetMergeBaseCommits(this IGitRepository repository, Commit mergeCommit, Commit mergedHead, Commit findMergeBase)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = mergedHead,
                ExcludeReachableFrom = findMergeBase
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            var commits = mergeCommit != null
                ? new[] { mergeCommit }.Union(commitCollection)
                : commitCollection;
            return commits.ToList();
        }

        public static Commit GetBaseVersionSource(this IGitRepository repository, Commit currentBranchTip)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = currentBranchTip
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            var baseVersionSource = commitCollection.First(c => !c.Parents.Any());
            return baseVersionSource;
        }

        public static List<Commit> GetMainlineCommitLog(this IGitRepository repository, Commit baseVersionSource, Commit mainlineTip)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = mainlineTip,
                ExcludeReachableFrom = baseVersionSource,
                SortBy = CommitSortStrategies.Reverse,
                FirstParentOnly = true
            };
            var commitCollection = repository.Commits.QueryBy(filter);

            var mainlineCommitLog = commitCollection.ToList();
            return mainlineCommitLog;
        }

        public static CommitCollection GetCommitLog(this IGitRepository repository, Commit baseVersionSource, Commit currentCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = currentCommit,
                ExcludeReachableFrom = baseVersionSource,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            var commitCollection = repository.Commits.QueryBy(filter);

            return commitCollection;
        }
    }
}
