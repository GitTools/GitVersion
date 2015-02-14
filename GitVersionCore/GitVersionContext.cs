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

        public GitVersionContext(IRepository repository, Config configuration, bool isForTrackingBranchOnly = true)
            : this(repository, repository.Head, configuration, isForTrackingBranchOnly)
        {
        }

        public GitVersionContext(IRepository repository, Branch currentBranch, Config configuration, bool onlyEvaluateTrackedBranches = true)
        {
            Repository = repository;
            this.configuration = configuration;
            OnlyEvaluateTrackedBranches = onlyEvaluateTrackedBranches;

            if (currentBranch == null)
                throw new InvalidOperationException("Need a branch to operate on");

            CurrentCommit = currentBranch.Tip;
            IsCurrentCommitTagged = repository.Tags.Any(t => t.Target == CurrentCommit);

            if (currentBranch.IsDetachedHead())
            {
                CurrentBranch = CurrentCommit.GetBranchesContainingCommit(repository, OnlyEvaluateTrackedBranches).OnlyOrDefault() ?? currentBranch;
            }
            else
            {
                CurrentBranch = currentBranch;
            }

            CalculateEffectiveConfiguration();
        }

        public bool OnlyEvaluateTrackedBranches { get; private set; }
        public EffectiveConfiguration Configuration { get; private set; }
        public IRepository Repository { get; private set; }
        public Branch CurrentBranch { get; private set; }
        public Commit CurrentCommit { get; private set; }
        public bool IsCurrentCommitTagged { get; private set; }

        void CalculateEffectiveConfiguration()
        {
            var currentBranchConfig = BranchConfigurationCalculator.GetBranchConfiguration(CurrentCommit, Repository, OnlyEvaluateTrackedBranches, configuration, CurrentBranch);

            // Versioning mode drills down, if top level is specified then it takes priority
            var versioningMode = configuration.VersioningMode ?? currentBranchConfig.Value.VersioningMode ?? VersioningMode.ContinuousDelivery;

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
                tagNumberPattern, configuration.ContinuousDeploymentFallbackTag);
        }
    }
}