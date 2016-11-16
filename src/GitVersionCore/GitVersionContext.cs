namespace GitVersion
{
    using LibGit2Sharp;
    using System;
    using System.Collections.Generic;
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
                CurrentBranch = CurrentCommit.GetBranchesContainingCommit(repository, repository.Branches.ToList(), OnlyEvaluateTrackedBranches).OnlyOrDefault() ?? currentBranch;
            }
            else
            {
                CurrentBranch = currentBranch;
            }

            Configurations = CalculateEffectiveConfiguration().ToArray();

            CurrentCommitTaggedVersion = repository.Tags
                .SelectMany(t =>
                {
                    SemanticVersion version;
                    if (t.PeeledTarget() == CurrentCommit && SemanticVersion.TryParse(t.FriendlyName, Configurations.First().GitTagPrefix, out version))
                        return new[] { version };
                    return new SemanticVersion[0];
                })
                .Max();
            IsCurrentCommitTagged = CurrentCommitTaggedVersion != null;
        }

        /// <summary>
        /// Contains the raw configuration, use Configurations for the specific configs based on the current GitVersion context.
        /// </summary>
        public Config FullConfiguration { get; private set; }

        public SemanticVersion CurrentCommitTaggedVersion { get; private set; }
        public bool OnlyEvaluateTrackedBranches { get; private set; }
        public ICollection<EffectiveConfiguration> Configurations { get; private set; }

        public IRepository Repository { get; private set; }
        public Branch CurrentBranch { get; private set; }
        public Commit CurrentCommit { get; private set; }
        public bool IsCurrentCommitTagged { get; private set; }

        IEnumerable<EffectiveConfiguration> CalculateEffectiveConfiguration()
        {
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

            var excludedBranches = new HashSet<Branch>();
            var firstCommit = CurrentCommit;
            var branch = CurrentBranch;

            var parentBranchesInfo = new List<ConfigInfo>();
            while (true)
            {
                var branchConfig = BranchConfigurationCalculator.GetBranchConfiguration(firstCommit, Repository, OnlyEvaluateTrackedBranches, FullConfiguration, branch, new HashSet<Branch>(excludedBranches));

                var firstParentBranch = BranchConfigurationCalculator.FindFirstParentBranch(Repository, firstCommit, branch, excludedBranches);
                if (firstParentBranch != BranchCommit.Empty)
                {
                    // A parent was found.
                    var configInfo = new ConfigInfo(branchConfig, branch, firstParentBranch.Branch, firstCommit, firstParentBranch.Commit, Repository);
                    parentBranchesInfo.Add(configInfo);

                    firstCommit = firstParentBranch.Commit;
                    branch = firstParentBranch.Branch;
                }
                else
                {
                    // No more parents => We are done.
                    var configInfo = new ConfigInfo(branchConfig, branch, null, firstCommit, null, Repository);
                    parentBranchesInfo.Add(configInfo);
                    break;
                }
            }

            foreach (var currentBranchInfo in parentBranchesInfo)
            {
                var currentBranchConfig = currentBranchInfo.Config;
                if (!currentBranchConfig.VersioningMode.HasValue)
                    throw new Exception(string.Format("Configuration value for 'Versioning mode' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
                if (!currentBranchConfig.Increment.HasValue)
                    throw new Exception(string.Format("Configuration value for 'Increment' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
                if (!currentBranchConfig.PreventIncrementOfMergedBranchVersion.HasValue)
                    throw new Exception(string.Format("Configuration value for 'PreventIncrementOfMergedBranchVersion' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
                if (!currentBranchConfig.TrackMergeTarget.HasValue)
                    throw new Exception(string.Format("Configuration value for 'TrackMergeTarget' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
                if (!currentBranchConfig.IsDevelop.HasValue)
                    throw new Exception(string.Format("Configuration value for 'IsDevelop' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));
                if (!currentBranchConfig.IsReleaseBranch.HasValue)
                    throw new Exception(string.Format("Configuration value for 'IsReleaseBranch' for branch {0} has no value. (this should not happen, please report an issue)", currentBranchConfig.Name));

                yield return new EffectiveConfiguration(
                    FullConfiguration.AssemblyVersioningScheme.Value,
                    FullConfiguration.AssemblyInformationalFormat,
                    currentBranchConfig.VersioningMode.Value,
                    FullConfiguration.TagPrefix,
                    currentBranchConfig.Tag,
                    FullConfiguration.NextVersion,
                    currentBranchConfig.Increment.Value,
                    currentBranchConfig.Regex,
                    currentBranchConfig.PreventIncrementOfMergedBranchVersion.Value,
                    currentBranchConfig.TagNumberPattern,
                    FullConfiguration.ContinuousDeploymentFallbackTag,
                    currentBranchConfig.TrackMergeTarget.Value,
                    FullConfiguration.MajorVersionBumpMessage,
                    FullConfiguration.MinorVersionBumpMessage,
                    FullConfiguration.PatchVersionBumpMessage,
                    FullConfiguration.NoBumpMessage,
                    currentBranchConfig.CommitMessageIncrementing ?? FullConfiguration.CommitMessageIncrementing.Value,
                    FullConfiguration.LegacySemVerPadding.Value,
                    FullConfiguration.BuildMetaDataPadding.Value,
                    FullConfiguration.CommitsSinceVersionSourcePadding.Value,
                    FullConfiguration.Ignore.ToFilters(),
                    currentBranchConfig.IsDevelop.Value,
                    currentBranchConfig.IsReleaseBranch.Value,
                    currentBranchInfo);
            }
        }
    }

    public class ConfigInfo
    {
        public ConfigInfo(BranchConfig config, Branch branch, Branch parentBranch, Commit firstCommit, Commit lastCommit, IRepository repository)
        {
            Config = config;
            Branch = branch;
            ParentBranch = parentBranch;
            FirstCommit = firstCommit;
            LastCommit = lastCommit;
            Repository = repository;
        }

        public BranchConfig Config { get; private set; }
        public Branch Branch { get; private set; }
        public Branch ParentBranch { get; private set; }
        public Commit FirstCommit { get; private set; }
        public Commit LastCommit { get; private set; }
        public IRepository Repository { get; private set; }

        /// <summary>
        /// Gets the relevant commits in the branch, i.e. all the commits between the <see cref="FirstCommit"/> and <see cref="LastCommit"/>.
        /// </summary>
        public IEnumerable<Commit> RelevantCommits
        {
            get
            {
                return Branch.Commits.SkipWhile(commit => commit != FirstCommit).TakeWhile(commit => commit != LastCommit);
            }
        }
    }
}
