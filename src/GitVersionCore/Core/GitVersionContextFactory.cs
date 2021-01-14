using System;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitVersionContextFactory : IGitVersionContextFactory
    {
        private readonly IConfigProvider configProvider;
        private readonly IRepositoryMetadataProvider repositoryMetadataProvider;
        private readonly IBranchConfigurationCalculator branchConfigurationCalculator;
        private readonly IOptions<GitVersionOptions> options;

        public GitVersionContextFactory(IConfigProvider configProvider, IRepositoryMetadataProvider repositoryMetadataProvider, IBranchConfigurationCalculator branchConfigurationCalculator, IOptions<GitVersionOptions> options)
        {
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            this.repositoryMetadataProvider = repositoryMetadataProvider ?? throw new ArgumentNullException(nameof(repositoryMetadataProvider));
            this.branchConfigurationCalculator = branchConfigurationCalculator ?? throw new ArgumentNullException(nameof(branchConfigurationCalculator));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public GitVersionContext Create(GitVersionOptions gitVersionOptions)
        {
            var currentBranch = repositoryMetadataProvider.GetTargetBranch(gitVersionOptions.RepositoryInfo.TargetBranch);
            if (currentBranch == null)
                throw new InvalidOperationException("Need a branch to operate on");

            var configuration = configProvider.Provide(overrideConfig: options.Value.ConfigInfo.OverrideConfig);

            var currentCommit = repositoryMetadataProvider.GetCurrentCommit(currentBranch, gitVersionOptions.RepositoryInfo.CommitId);

            if (currentBranch.IsDetachedHead)
            {
                var branchForCommit = repositoryMetadataProvider.GetBranchesContainingCommit(currentCommit, onlyTrackedBranches: gitVersionOptions.Settings.OnlyTrackedBranches).OnlyOrDefault();
                currentBranch = branchForCommit ?? currentBranch;
            }

            var currentBranchConfig = branchConfigurationCalculator.GetBranchConfiguration(currentBranch, currentCommit, configuration);
            var effectiveConfiguration = configuration.CalculateEffectiveConfiguration(currentBranchConfig);
            var currentCommitTaggedVersion = repositoryMetadataProvider.GetCurrentCommitTaggedVersion(currentCommit, effectiveConfiguration);
            var numberOfUncommittedChanges = repositoryMetadataProvider.GetNumberOfUncommittedChanges();

            return new GitVersionContext(currentBranch, currentCommit, configuration, effectiveConfiguration, currentCommitTaggedVersion, numberOfUncommittedChanges);
        }
    }
}
