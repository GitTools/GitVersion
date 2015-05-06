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
        readonly Config configuration;

        public GitVersionContext(IRepository repository, Config configuration, bool isForTrackingBranchOnly = true, string commitId = null)
            : this(repository, repository.Head, configuration, isForTrackingBranchOnly, commitId)
        {
        }

        public GitVersionContext(IRepository repository, Branch currentBranch, Config configuration, bool onlyEvaluateTrackedBranches = true, string commitId = null)
        {
            Repository = repository;
            this.configuration = configuration;
            OnlyEvaluateTrackedBranches = onlyEvaluateTrackedBranches;

            if (currentBranch == null)
                throw new InvalidOperationException("Need a branch to operate on");

            if (!string.IsNullOrWhiteSpace(commitId))
            {
                Logger.WriteInfo(string.Format("Searching for specific commit '{0}'", commitId));

                var commit = repository.Commits.FirstOrDefault(c => string.Equals(c.Sha, commitId, StringComparison.OrdinalIgnoreCase));
                if (commit != null)
                {
                    CurrentCommit = commit;
                }
            }

            if (CurrentCommit == null)
            {
                Logger.WriteWarning("No specific commit specified or found, falling back to latest commit on specified branch");

                CurrentCommit = currentBranch.Tip;
            }

            if (currentBranch.IsDetachedHead())
            {
                var branchesContainingCommit = CurrentCommit.GetBranchesContainingCommit(repository, OnlyEvaluateTrackedBranches).ToArray();
                if (branchesContainingCommit.Count() == 1)
                {
                    CurrentBranch = branchesContainingCommit[0];
                }
                else
                {
                    var bestGuess = TryAndGuessNextBestName(branchesContainingCommit, configuration);
                    if (bestGuess != null)
                    {
                        CurrentBranch = bestGuess;
                    }
                    else
                    {
                        CurrentBranch = currentBranch;
                    }
                }
            }
            else
            {
                CurrentBranch = currentBranch;
            }

            CalculateEffectiveConfiguration();

            CurrentCommitTaggedVersion = repository.Tags
                .SelectMany(t =>
                {
                    SemanticVersion version;
                    if (t.PeeledTarget() == CurrentCommit && SemanticVersion.TryParse(t.Name, Configuration.GitTagPrefix, out version))
                        return new[] { version };
                    return new SemanticVersion[0];
                })
                .Max();
            IsCurrentCommitTagged = CurrentCommitTaggedVersion != null;
        }

        Branch TryAndGuessNextBestName(Branch[] branches, Config config)
        {
            var items = new List<Tuple<string, BranchConfig, Branch>>();
            foreach (var branch in branches)
            {
                var matchingBranches = config.Branches.Where(b => Regex.IsMatch(branch.Name, "^" + b.Key, RegexOptions.IgnoreCase)).ToArray();
                items.AddRange(matchingBranches.Select(x=>new Tuple<string,BranchConfig, Branch>(x.Key, x.Value, branch)));
                var matchingRemoteBranches = config.Branches.Where(b => Regex.IsMatch(branch.Name, "^origin/" + b.Key, RegexOptions.IgnoreCase)).ToArray();
                items.AddRange(matchingRemoteBranches.Select(x=>new Tuple<string,BranchConfig, Branch>(x.Key, x.Value, branch)));
            }
            var selectionAction = new Func<string, List<Tuple<string, BranchConfig, Branch>>, Branch>((key, y) =>
            {
                var selectedItem = y.FirstOrDefault(x => x.Item1 == key);
                if (selectedItem != null)
                {
                    return selectedItem.Item3;
                }
                return null;
            });

            var branchTypes = new[]{
                "master",
                "release",
                "hotfix",
                "develop",
                "support",
                "feature"
            };
            foreach (var branchType in branchTypes)
            {
                var selectedItem = selectionAction(branchType, items);
                if (selectedItem != null)
                {
                    return selectedItem;
                }
            }
            return null;
        }

        public SemanticVersion CurrentCommitTaggedVersion { get; private set; }
        public bool OnlyEvaluateTrackedBranches { get; private set; }
        public EffectiveConfiguration Configuration { get; private set; }
        public IRepository Repository { get; private set; }
        public Branch CurrentBranch { get; private set; }
        public Commit CurrentCommit { get; private set; }
        public bool IsCurrentCommitTagged { get; private set; }

        void CalculateEffectiveConfiguration()
        {
            var currentBranchConfig = BranchConfigurationCalculator.GetBranchConfiguration(CurrentCommit, Repository, OnlyEvaluateTrackedBranches, configuration, CurrentBranch);
            
            var versioningMode = currentBranchConfig.Value.VersioningMode ?? configuration.VersioningMode ?? VersioningMode.ContinuousDelivery;
            var tag = currentBranchConfig.Value.Tag ?? "useBranchName";
            var nextVersion = configuration.NextVersion;
            var incrementStrategy = currentBranchConfig.Value.Increment ?? IncrementStrategy.Patch;
            var preventIncrementForMergedBranchVersion = currentBranchConfig.Value.PreventIncrementOfMergedBranchVersion ?? false;
            var assemblyVersioningScheme = configuration.AssemblyVersioningScheme;
            var gitTagPrefix = configuration.TagPrefix;
            var tagNumberPattern = currentBranchConfig.Value.TagNumberPattern;
            Configuration = new EffectiveConfiguration(
                assemblyVersioningScheme, versioningMode, gitTagPrefix, 
                tag, nextVersion, incrementStrategy, currentBranchConfig.Key, 
                preventIncrementForMergedBranchVersion, 
                tagNumberPattern, configuration.ContinuousDeploymentFallbackTag,
                currentBranchConfig.Value.TrackMergeTarget);
        }
    }
}