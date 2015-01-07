namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
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

            AssignBranchConfiguration();
        }

        public Config Configuration { get; private set; }
        public IRepository Repository { get; private set; }
        public Branch CurrentBranch { get; private set; }
        public Commit CurrentCommit { get; private set; }

        public BranchConfig CurrentBranchConfig { get; private set; }

        readonly bool IsContextForTrackedBranchesOnly = true;


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

        void AssignBranchConfiguration()
        {
            var matchingBranches = Configuration.Branches.Where(b => Regex.IsMatch(CurrentBranch.Name, b.Key)).ToArray();

            if (matchingBranches.Length == 0)
            {
                CurrentBranchConfig = new BranchConfig();
            }
            else if (matchingBranches.Length == 1)
            {
                CurrentBranchConfig = matchingBranches[0].Value;
            }
            else
            {
                throw new Exception(string.Format("Multiple branch configurations match the current branch name of ''. Matching configurations: '{0}'", string.Join(", ", matchingBranches.Select(b => b.Key))));
            }
        }
    }
}