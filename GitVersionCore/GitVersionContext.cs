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
        readonly bool IsContextForTrackedBranchesOnly;
        readonly Config configuration;

        public GitVersionContext(IRepository repository, Config configuration, bool isForTrackingBranchOnly = true)
            : this(repository, repository.Head, configuration, isForTrackingBranchOnly)
        {
        }

        public GitVersionContext(IRepository repository, Branch currentBranch, Config configuration, bool isForTrackingBranchOnly = true)
        {
            Repository = repository;
            this.configuration = configuration;
            IsContextForTrackedBranchesOnly = isForTrackingBranchOnly;

            if (currentBranch == null)
                throw new InvalidOperationException("Need a branch to operate on");

            CurrentCommit = currentBranch.Tip;

            if (repository != null && currentBranch.IsDetachedHead())
            {
                CurrentBranch = GetBranchesContainingCommit(CurrentCommit.Sha).OnlyOrDefault() ?? currentBranch;
            }
            else
            {
                CurrentBranch = currentBranch;
            }

            CalculateEffectiveConfiguration();
        }

        public EffectiveConfiguration Configuration { get; private set; }
        public IRepository Repository { get; private set; }
        public Branch CurrentBranch { get; private set; }
        public Commit CurrentCommit { get; private set; }

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

        void CalculateEffectiveConfiguration()
        {
            var matchingBranches = configuration.Branches.Where(b => Regex.IsMatch("^" + CurrentBranch.Name, b.Key)).ToArray();

            var currentBranchConfig = GetBranchConfiguration(matchingBranches);

            var versioningMode = currentBranchConfig.VersioningMode ?? configuration.VersioningMode ?? VersioningMode.ContinuousDelivery;
            var tag = currentBranchConfig.Tag;
            var nextVersion = configuration.NextVersion;

            Configuration = new EffectiveConfiguration(configuration.AssemblyVersioningScheme, versioningMode, configuration.TagPrefix, tag, nextVersion);
        }

        BranchConfig GetBranchConfiguration(KeyValuePair<string, BranchConfig>[] matchingBranches)
        {
            if (matchingBranches.Length == 0)
            {
                return new BranchConfig();
            }
            if (matchingBranches.Length == 1)
            {
                return matchingBranches[0].Value;
            }

            const string format = "Multiple branch configurations match the current branch name of '{0}'. Matching configurations: '{1}'";
            throw new Exception(string.Format(format, CurrentBranch.Name, string.Join(", ", matchingBranches.Select(b => b.Key))));
        }
    }
}