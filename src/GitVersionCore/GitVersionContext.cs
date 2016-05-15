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
                else
                {
                    Logger.WriteWarning(string.Format("Commit '{0}' specified but not found", commitId));
                }
            }

            if (CurrentCommit == null)
            {
                Logger.WriteInfo("Using latest commit on specified branch");
                CurrentCommit = currentBranch.Tip;
            }

            if (currentBranch.IsDetachedHead())
            {
                CurrentBranch = CurrentCommit.GetBranchesContainingCommit(repository, repository.Branches.ToList(), OnlyEvaluateTrackedBranches).OnlyOrDefault() ?? currentBranch;
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
                    if (t.PeeledTarget() == CurrentCommit && SemanticVersion.TryParse(t.FriendlyName, Configuration.GitTagPrefix, out version))
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

            if (!currentBranchConfig.Value.VersioningMode.HasValue)
                throw new Exception(string.Format("Configuration value for 'Versioning mode' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Key));
            if (!currentBranchConfig.Value.Increment.HasValue)
                throw new Exception(string.Format("Configuration value for 'Increment' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Key));
            if (!currentBranchConfig.Value.PreventIncrementOfMergedBranchVersion.HasValue)
                throw new Exception(string.Format("Configuration value for 'PreventIncrementOfMergedBranchVersion' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Key));
            if (!currentBranchConfig.Value.TrackMergeTarget.HasValue)
                throw new Exception(string.Format("Configuration value for 'TrackMergeTarget' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Key));
            if (!configuration.AssemblyVersioningScheme.HasValue)
                throw new Exception("Configuration value for 'AssemblyVersioningScheme' has no value. (this should not happen, please report an issue)");
            if (!configuration.CommitMessageIncrementing.HasValue)
                throw new Exception("Configuration value for 'CommitMessageIncrementing' has no value. (this should not happen, please report an issue)");

            var versioningMode = currentBranchConfig.Value.VersioningMode.Value;
            var tag = currentBranchConfig.Value.Tag;
            var tagNumberPattern = currentBranchConfig.Value.TagNumberPattern;
            var incrementStrategy = currentBranchConfig.Value.Increment.Value;
            var preventIncrementForMergedBranchVersion = currentBranchConfig.Value.PreventIncrementOfMergedBranchVersion.Value;
            var trackMergeTarget = currentBranchConfig.Value.TrackMergeTarget.Value;

            var nextVersion = configuration.NextVersion;
            var assemblyVersioningScheme = configuration.AssemblyVersioningScheme.Value;
            var assemblyInformationalFormat = configuration.AssemblyInformationalFormat;
            var gitTagPrefix = configuration.TagPrefix;
            var majorMessage = configuration.MajorVersionBumpMessage;
            var minorMessage = configuration.MinorVersionBumpMessage;
            var patchMessage = configuration.PatchVersionBumpMessage;

            var commitMessageVersionBump = currentBranchConfig.Value.CommitMessageIncrementing ?? configuration.CommitMessageIncrementing.Value;

            var versionFilter =
            Configuration = new EffectiveConfiguration(
                assemblyVersioningScheme, assemblyInformationalFormat, versioningMode, gitTagPrefix,
                tag, nextVersion, incrementStrategy, currentBranchConfig.Key,
                preventIncrementForMergedBranchVersion,
                tagNumberPattern, configuration.ContinuousDeploymentFallbackTag,
                trackMergeTarget,
                majorMessage, minorMessage, patchMessage,
                commitMessageVersionBump,
                configuration.LegacySemVerPadding.Value,
                configuration.BuildMetaDataPadding.Value,
                configuration.CommitsSinceVersionSourcePadding.Value,
                configuration.Ignore.ToFilters()
                );
        }
    }
}
