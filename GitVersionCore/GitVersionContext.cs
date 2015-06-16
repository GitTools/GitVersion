namespace GitVersion
{
    using System;
    using System.Linq;
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
                CurrentBranch = CurrentCommit.GetBranchesContainingCommit(repository, OnlyEvaluateTrackedBranches).OnlyOrDefault() ?? currentBranch;
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
                currentBranchConfig.Value.TrackMergeTarget,
                configuration.CommitsToIgnore,
                configuration.MergeMessagesToIgnore);
        }
    }
}