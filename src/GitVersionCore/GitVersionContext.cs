﻿namespace GitVersion
{
    using LibGit2Sharp;
    using System;
    using System.Linq;

    /// <summary>
    /// Contextual information about where GitVersion is being run
    /// </summary>
    public class GitVersionContext
    {
        public GitVersionContext(IRepository repository, Config configuration, bool isForTrackingBranchOnly = true, string commitId = null)
            : this(repository, repository.Head, configuration, isForTrackingBranchOnly, commitId)
        {
        }

        public GitVersionContext(IRepository repository, Branch currentBranch, Config configuration, bool onlyEvaluateTrackedBranches = true, string commitId = null)
        {
            Repository = repository;
            RepositoryMetadataProvider = new GitRepoMetadataProvider(repository);
            FullConfiguration = configuration;
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
                CurrentBranch = RepositoryMetadataProvider.GetBranchesContainingCommit(CurrentCommit, repository.Branches.ToList(), OnlyEvaluateTrackedBranches).OnlyOrDefault() ?? currentBranch;
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

        /// <summary>
        /// Contains the raw configuration, use Configuration for specific config based on the current GitVersion context.
        /// </summary>
        public Config FullConfiguration { get; private set; }
        public SemanticVersion CurrentCommitTaggedVersion { get; private set; }
        public bool OnlyEvaluateTrackedBranches { get; private set; }
        public EffectiveConfiguration Configuration { get; private set; }
        public IRepository Repository { get; private set; }
        public Branch CurrentBranch { get; private set; }
        public Commit CurrentCommit { get; private set; }
        public bool IsCurrentCommitTagged { get; private set; }
        public GitRepoMetadataProvider RepositoryMetadataProvider { get; private set; }

        void CalculateEffectiveConfiguration()
        {
            var currentBranchConfig = BranchConfigurationCalculator.GetBranchConfiguration(this, CurrentBranch);

            if (!currentBranchConfig.VersioningMode.HasValue)
                throw new Exception(string.Format("Configuration value for 'Versioning mode' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
            if (!currentBranchConfig.Increment.HasValue)
                throw new Exception(string.Format("Configuration value for 'Increment' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
            if (!currentBranchConfig.PreventIncrementOfMergedBranchVersion.HasValue)
                throw new Exception(string.Format("Configuration value for 'PreventIncrementOfMergedBranchVersion' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
            if (!currentBranchConfig.TrackMergeTarget.HasValue)
                throw new Exception(string.Format("Configuration value for 'TrackMergeTarget' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
            if (!currentBranchConfig.TracksReleaseBranches.HasValue)
                throw new Exception(string.Format("Configuration value for 'TracksReleaseBranches' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
            if (!currentBranchConfig.IsReleaseBranch.HasValue)
                throw new Exception(string.Format("Configuration value for 'IsReleaseBranch' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));

            if (!FullConfiguration.AssemblyVersioningScheme.HasValue)
                throw new Exception("Configuration value for 'AssemblyVersioningScheme' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.CommitMessageIncrementing.HasValue)
                throw new Exception("Configuration value for 'CommitMessageIncrementing' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.LegacySemVerPadding.HasValue)
                throw new Exception("Configuration value for 'LegacySemVerPadding' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.BuildMetaDataPadding.HasValue)
                throw new Exception("Configuration value for 'BuildMetaDataPadding' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.CommitsSinceVersionSourcePadding.HasValue)
                throw new Exception("Configuration value for 'CommitsSinceVersionSourcePadding' has no value. (this should not happen, please report an issue)");

            var versioningMode = currentBranchConfig.VersioningMode.Value;
            var tag = currentBranchConfig.Tag;
            var tagNumberPattern = currentBranchConfig.TagNumberPattern;
            var incrementStrategy = currentBranchConfig.Increment.Value;
            var preventIncrementForMergedBranchVersion = currentBranchConfig.PreventIncrementOfMergedBranchVersion.Value;
            var trackMergeTarget = currentBranchConfig.TrackMergeTarget.Value;

            var nextVersion = FullConfiguration.NextVersion;
            var assemblyVersioningScheme = FullConfiguration.AssemblyVersioningScheme.Value;
            var assemblyInformationalFormat = FullConfiguration.AssemblyInformationalFormat;
            var gitTagPrefix = FullConfiguration.TagPrefix;
            var majorMessage = FullConfiguration.MajorVersionBumpMessage;
            var minorMessage = FullConfiguration.MinorVersionBumpMessage;
            var patchMessage = FullConfiguration.PatchVersionBumpMessage;
            var noBumpMessage = FullConfiguration.NoBumpMessage;

            var commitMessageVersionBump = currentBranchConfig.CommitMessageIncrementing ?? FullConfiguration.CommitMessageIncrementing.Value;

            Configuration = new EffectiveConfiguration(
                assemblyVersioningScheme, assemblyInformationalFormat, versioningMode, gitTagPrefix,
                tag, nextVersion, incrementStrategy,
                currentBranchConfig.Regex,
                preventIncrementForMergedBranchVersion,
                tagNumberPattern, FullConfiguration.ContinuousDeploymentFallbackTag,
                trackMergeTarget,
                majorMessage, minorMessage, patchMessage, noBumpMessage,
                commitMessageVersionBump,
                FullConfiguration.LegacySemVerPadding.Value,
                FullConfiguration.BuildMetaDataPadding.Value,
                FullConfiguration.CommitsSinceVersionSourcePadding.Value,
                FullConfiguration.Ignore.ToFilters(),
                currentBranchConfig.TracksReleaseBranches.Value,
                currentBranchConfig.IsReleaseBranch.Value);
        }
    }
}
