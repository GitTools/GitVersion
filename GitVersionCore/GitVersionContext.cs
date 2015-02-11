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
            var currentBranchConfig = GetBranchConfiguration(CurrentBranch);

            // Versioning mode drills down, if top level is specified then it takes priority
            var versioningMode = configuration.VersioningMode ?? currentBranchConfig.Value.VersioningMode ?? VersioningMode.ContinuousDelivery;

            var tag = currentBranchConfig.Value.Tag ?? "useBranchName";
            var nextVersion = configuration.NextVersion;
            var incrementStrategy = currentBranchConfig.Value.Increment ?? IncrementStrategy.Patch;
            var assemblyVersioningScheme = configuration.AssemblyVersioningScheme;
            var gitTagPrefix = configuration.TagPrefix;
            Configuration = new EffectiveConfiguration(assemblyVersioningScheme, versioningMode, gitTagPrefix, tag, nextVersion, incrementStrategy, currentBranchConfig.Key);
        }

        KeyValuePair<string, BranchConfig> GetBranchConfiguration(Branch currentBranch)
        {
            var matchingBranches = configuration.Branches.Where(b => Regex.IsMatch("^" + currentBranch.Name, b.Key)).ToArray();

            if (matchingBranches.Length == 0)
            {
                return new KeyValuePair<string, BranchConfig>(string.Empty, new BranchConfig());
            }
            if (matchingBranches.Length == 1)
            {
                var keyValuePair = matchingBranches[0];
                var branchConfiguration = keyValuePair.Value;

                if (branchConfiguration.Increment == IncrementStrategy.Inherit)
                {
                    return InheritBranchConfiguration(currentBranch, keyValuePair, branchConfiguration);
                }

                return keyValuePair;
            }

            const string format = "Multiple branch configurations match the current branch branchName of '{0}'. Matching configurations: '{1}'";
            throw new Exception(string.Format(format, currentBranch.Name, string.Join(", ", matchingBranches.Select(b => b.Key))));
        }

        KeyValuePair<string, BranchConfig> InheritBranchConfiguration(Branch currentBranch, KeyValuePair<string, BranchConfig> keyValuePair, BranchConfig branchConfiguration)
        {
            var excludedBranches = new Branch[0];
            // Check if we are a merge commit. If so likely we are a pull request
            var parentCount = CurrentCommit.Parents.Count();
            if (parentCount == 2)
            {
                var parents = CurrentCommit.Parents.ToArray();
                var branch = Repository.Branches.SingleOrDefault(b => !b.IsRemote && b.Tip == parents[1]) ;
                if (branch != null)
                {
                    excludedBranches = new[]
                    {
                        currentBranch,
                        branch
                    };
                    currentBranch = branch;
                }
                else
                {
                    currentBranch = Repository.Branches.SingleOrDefault(b => !b.IsRemote && b.Tip == parents[0]) ?? currentBranch;
                }
            }

            var branchPoint = currentBranch.FindCommitBranchWasBranchedFrom(Repository, excludedBranches);

            List<Branch> possibleParents;
            if (branchPoint.Sha == CurrentCommit.Sha)
            {
                possibleParents = ListBranchesContaininingCommit(Repository, CurrentCommit.Sha, excludedBranches).Except(new[]
                {
                    currentBranch
                }).ToList();
            }
            else
            {
                var branches = ListBranchesContaininingCommit(Repository, branchPoint.Sha, excludedBranches).ToArray();
                var currentTipBranches = ListBranchesContaininingCommit(Repository, CurrentCommit.Sha, excludedBranches).ToArray();
                possibleParents = branches
                    .Except(currentTipBranches)
                    .ToList();
            }

            // If it comes down to master and something, master is always first so we pick other branch
            if (possibleParents.Count == 2 && possibleParents.Any(p => p.Name == "master"))
            {
                possibleParents.Remove(possibleParents.Single(p => p.Name == "master"));
            }

            if (possibleParents.Count == 1)
            {
                return new KeyValuePair<string, BranchConfig>(
                    keyValuePair.Key,
                    new BranchConfig(branchConfiguration)
                    {
                        Increment = GetBranchConfiguration(possibleParents[0]).Value.Increment
                    });
            }

            throw new Exception("Failed to inherit Increment branch configuration");
        }

        static IEnumerable<Branch> ListBranchesContaininingCommit(IRepository repo, string commitSha, Branch[] excludedBranches)
        {
            return from branch in repo.Branches.Except(excludedBranches)
                   where !branch.IsRemote
                   let commits = repo.Commits.QueryBy(new CommitFilter { Since = branch }).Where(c => c.Sha == commitSha)
                   where commits.Any()
                   select branch;
        }
    }
}