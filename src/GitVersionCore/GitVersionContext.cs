using LibGit2Sharp;
using System;
using System.Linq;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.Extensions;

namespace GitVersion
{
    /// <summary>
    /// Contextual information about where GitVersion is being run
    /// </summary>
    public class GitVersionContext
    {
        public ILog Log { get; }

        public GitVersionContext(IRepository repository, ILog log, string targetBranch, Config configuration, bool onlyEvaluateTrackedBranches = true, string commitId = null)
             : this(repository, log, GetTargetBranch(repository, targetBranch), configuration, onlyEvaluateTrackedBranches, commitId)
        {
        }

        public GitVersionContext(IRepository repository, ILog log, Branch currentBranch, Config configuration, bool onlyEvaluateTrackedBranches = true, string commitId = null)
        {
            Log = log;
            Repository = repository;
            RepositoryMetadataProvider = new GitRepoMetadataProvider(repository, log, configuration);
            FullConfiguration = configuration;
            OnlyEvaluateTrackedBranches = onlyEvaluateTrackedBranches;

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
        public bool OnlyEvaluateTrackedBranches { get; }
        public EffectiveConfiguration Configuration { get; private set; }
        public IRepository Repository { get; }
        public Branch CurrentBranch { get; }
        public Commit CurrentCommit { get; }
        public bool IsCurrentCommitTagged { get; }
        public GitRepoMetadataProvider RepositoryMetadataProvider { get; }

        private void CalculateEffectiveConfiguration()
        {
            IBranchConfigurationCalculator calculator = new BranchConfigurationCalculator(Log, this);
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

        private static Branch GetTargetBranch(IRepository repository, string targetBranch)
        {
            // By default, we assume HEAD is pointing to the desired branch
            var desiredBranch = repository.Head;

            // Make sure the desired branch has been specified
            if (!string.IsNullOrEmpty(targetBranch))
            {
                // There are some edge cases where HEAD is not pointing to the desired branch.
                // Therefore it's important to verify if 'currentBranch' is indeed the desired branch.

                // CanonicalName can be "refs/heads/develop", so we need to check for "/{TargetBranch}" as well
                if (!desiredBranch.CanonicalName.IsBranch(targetBranch))
                {
                    // In the case where HEAD is not the desired branch, try to find the branch with matching name
                    desiredBranch = repository?.Branches?
                        .SingleOrDefault(b =>
                            b.CanonicalName == targetBranch ||
                            b.FriendlyName == targetBranch ||
                            b.NameWithoutRemote() == targetBranch);

                    // Failsafe in case the specified branch is invalid
                    desiredBranch ??= repository.Head;
                }
            }

            return desiredBranch;
        }
    }
}
