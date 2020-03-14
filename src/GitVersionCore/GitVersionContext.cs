using System;
using System.Linq;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using LibGit2Sharp;

namespace GitVersion
{
    /// <summary>
    /// Contextual information about where GitVersion is being run
    /// </summary>
    public class GitVersionContext
    {
        private readonly ILog log;

        public GitVersionContext(IRepository repository, ILog log, Branch currentBranch, Config configuration, bool onlyTrackedBranches = false, string commitId = null)
        {
            this.log = log;
            Repository = repository;
            RepositoryMetadataProvider = new GitRepoMetadataProvider(repository, log, configuration);
            FullConfiguration = configuration;
            OnlyTrackedBranches = onlyTrackedBranches;

            if (currentBranch == null)
                throw new InvalidOperationException("Need a branch to operate on");

            if (!string.IsNullOrWhiteSpace(commitId))
            {
                log.Info($"Searching for specific commit '{commitId}'");

                var commit = repository.Commits.FirstOrDefault(c => string.Equals(c.Sha, commitId, StringComparison.OrdinalIgnoreCase));
                if (commit != null)
                {
                    CurrentCommit = commit;
                }
                else
                {
                    log.Warning($"Commit '{commitId}' specified but not found");
                }
            }

            if (CurrentCommit == null)
            {
                log.Info("Using latest commit on specified branch");
                CurrentCommit = currentBranch.Tip;
            }

            if (currentBranch.IsDetachedHead())
            {
                CurrentBranch = RepositoryMetadataProvider.GetBranchesContainingCommit(CurrentCommit, repository.Branches.ToList(), OnlyTrackedBranches).OnlyOrDefault() ?? currentBranch;
            }
            else
            {
                CurrentBranch = currentBranch;
            }

            CalculateEffectiveConfiguration();

            CurrentCommitTaggedVersion = repository.Tags
                .SelectMany(t =>
                {
                    if (t.PeeledTarget() == CurrentCommit && SemanticVersion.TryParse(t.FriendlyName, Configuration.GitTagPrefix, out var version))
                        return new[] { version };
                    return new SemanticVersion[0];
                })
                .Max();
            IsCurrentCommitTagged = CurrentCommitTaggedVersion != null;
        }

        /// <summary>
        /// Contains the raw configuration, use Configuration for specific config based on the current GitVersion context.
        /// </summary>
        public Config FullConfiguration { get; }
        public SemanticVersion CurrentCommitTaggedVersion { get; }
        public bool OnlyTrackedBranches { get; }
        public EffectiveConfiguration Configuration { get; private set; }
        public IRepository Repository { get; }
        public Branch CurrentBranch { get; }
        public Commit CurrentCommit { get; }
        public bool IsCurrentCommitTagged { get; }
        public GitRepoMetadataProvider RepositoryMetadataProvider { get; }

        private void CalculateEffectiveConfiguration()
        {
            IBranchConfigurationCalculator calculator = new BranchConfigurationCalculator(log, this);
            var currentBranchConfig = calculator.GetBranchConfiguration(CurrentBranch);

            if (!currentBranchConfig.VersioningMode.HasValue)
                throw new Exception($"Configuration value for 'Versioning mode' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.Increment.HasValue)
                throw new Exception($"Configuration value for 'Increment' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.PreventIncrementOfMergedBranchVersion.HasValue)
                throw new Exception($"Configuration value for 'PreventIncrementOfMergedBranchVersion' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.TrackMergeTarget.HasValue)
                throw new Exception($"Configuration value for 'TrackMergeTarget' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.TracksReleaseBranches.HasValue)
                throw new Exception($"Configuration value for 'TracksReleaseBranches' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");
            if (!currentBranchConfig.IsReleaseBranch.HasValue)
                throw new Exception($"Configuration value for 'IsReleaseBranch' for branch {currentBranchConfig.Name} has no value. (this should not happen, please report an issue)");

            if (!FullConfiguration.AssemblyVersioningScheme.HasValue)
                throw new Exception("Configuration value for 'AssemblyVersioningScheme' has no value. (this should not happen, please report an issue)");
            if (!FullConfiguration.AssemblyFileVersioningScheme.HasValue)
                throw new Exception("Configuration value for 'AssemblyFileVersioningScheme' has no value. (this should not happen, please report an issue)");
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
            var preReleaseWeight = currentBranchConfig.PreReleaseWeight ?? 0;

            var nextVersion = FullConfiguration.NextVersion;
            var assemblyVersioningScheme = FullConfiguration.AssemblyVersioningScheme.Value;
            var assemblyFileVersioningScheme = FullConfiguration.AssemblyFileVersioningScheme.Value;
            var assemblyInformationalFormat = FullConfiguration.AssemblyInformationalFormat;
            var assemblyVersioningFormat = FullConfiguration.AssemblyVersioningFormat;
            var assemblyFileVersioningFormat = FullConfiguration.AssemblyFileVersioningFormat;
            var gitTagPrefix = FullConfiguration.TagPrefix;
            var majorMessage = FullConfiguration.MajorVersionBumpMessage;
            var minorMessage = FullConfiguration.MinorVersionBumpMessage;
            var patchMessage = FullConfiguration.PatchVersionBumpMessage;
            var noBumpMessage = FullConfiguration.NoBumpMessage;
            var commitDateFormat = FullConfiguration.CommitDateFormat;

            var commitMessageVersionBump = currentBranchConfig.CommitMessageIncrementing ?? FullConfiguration.CommitMessageIncrementing.Value;

            Configuration = new EffectiveConfiguration(
                assemblyVersioningScheme, assemblyFileVersioningScheme, assemblyInformationalFormat, assemblyVersioningFormat, assemblyFileVersioningFormat, versioningMode, gitTagPrefix,
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
                currentBranchConfig.IsReleaseBranch.Value,
                commitDateFormat,
                preReleaseWeight);
        }
    }
}
