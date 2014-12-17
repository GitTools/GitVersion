namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using LibGit2Sharp;

    /// <summary>
    /// Contextual information about where GitVersion is being run
    /// </summary>
    public class GitVersionContext
    {
        public GitVersionContext(IRepository repository, Config configuration, bool isForTrackingBranchOnly = true)
            : this(repository, repository.Head, configuration, isForTrackingBranchOnly)
        {
            Configuration = configuration;
        }

        public GitVersionContext(IRepository repository, Branch currentBranch, Config configuration, bool isForTrackingBranchOnly = true)
        {
            Repository = repository;
            Configuration = configuration;
            IsContextForTrackedBranchesOnly = isForTrackingBranchOnly;

            if (currentBranch == null)
                return;

            CurrentCommit = currentBranch.Tip;

            if (repository != null && currentBranch.IsDetachedHead())
            {
                CurrentBranch = GetBranchesContainingCommit(CurrentCommit.Sha).OnlyOrDefault() ?? currentBranch;
            }
            else
            {
                CurrentBranch = currentBranch;
            }
        }

        public Config Configuration { get; private set; }
        public IRepository Repository { get; private set; }
        public Branch CurrentBranch { get; private set; }
        public Commit CurrentCommit { get; private set; }

        public bool IsContextForTrackedBranchesOnly = true;


        IEnumerable<Branch> GetBranchesContainingCommit(string commitSha)
        {
            var directBranchHasBeenFound = false;
            foreach (var branch in Repository.Branches)
            {
                if (branch.Tip.Sha != commitSha || (IsContextForTrackedBranchesOnly && !branch.IsTracking))
                {
                    continue;
                }

                directBranchHasBeenFound = true;
                yield return branch;
            }

            if (directBranchHasBeenFound)
            {
                yield break;
            }

            foreach (var branch in Repository.Branches)
            {
                var commits = Repository.Commits.QueryBy(new CommitFilter { Since = branch }).Where(c => c.Sha == commitSha);

                if (!commits.Any())
                {
                    continue;
                }

                yield return branch;
            }
        }
    }
}