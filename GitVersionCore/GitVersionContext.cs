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

            var versioningMode = currentBranchConfig.Value.VersioningMode ?? configuration.VersioningMode ?? VersioningMode.ContinuousDelivery;
            var tag = currentBranchConfig.Value.Tag;
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
                    var branchPoint = currentBranch.FindCommitBranchWasBranchedFrom(Repository);

                    List<Branch> possibleParents;
                    if (branchPoint.Sha == CurrentCommit.Sha)
                    {
                        possibleParents = ListBranchesContaininingCommit(Repository, CurrentCommit.Sha).Except(new[]
                        {
                            currentBranch
                        }).ToList();
                    }
                    else
                    {
                        var branches = ListBranchesContaininingCommit(Repository, branchPoint.Sha).ToArray();
                        var currentTipBranches = ListBranchesContaininingCommit(Repository, CurrentCommit.Sha).ToArray();
                        possibleParents = branches
                            .Except(currentTipBranches)
                            .ToList();
                    }

                    // If it comes down to master and something, master is always first so we pick other branch
                    if (possibleParents.Count == 2 && possibleParents.Any(p => p.Name == "master"))
                        possibleParents.Remove(possibleParents.Single(p => p.Name == "master"));

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

                return keyValuePair;
            }

            const string format = "Multiple branch configurations match the current branch branchName of '{0}'. Matching configurations: '{1}'";
            throw new Exception(string.Format(format, currentBranch.Name, string.Join(", ", matchingBranches.Select(b => b.Key))));
        }

        static IEnumerable<Branch> ListBranchesContaininingCommit(IRepository repo, string commitSha)
        {
            return from branch in repo.Branches
                   where !branch.IsRemote
                   let commits = repo.Commits.QueryBy(new CommitFilter { Since = branch }).Where(c => c.Sha == commitSha)
                   where commits.Any()
                   select branch;
        }
    }
}