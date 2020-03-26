using System;
using System.Linq;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitVersionContextFactory : IGitVersionContextFactory
    {
        private readonly ILog log;
        private readonly IRepository repository;
        private readonly IConfigProvider configProvider;
        private readonly IGitRepoMetadataProvider gitRepoMetadataProvider;
        private readonly IBranchConfigurationCalculator branchConfigurationCalculator;
        private readonly IOptions<Arguments> options;

        public GitVersionContextFactory(ILog log, IRepository repository, IConfigProvider configProvider, IGitRepoMetadataProvider gitRepoMetadataProvider, IBranchConfigurationCalculator branchConfigurationCalculator, IOptions<Arguments> options)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.repository = repository?? throw new ArgumentNullException(nameof(repository));
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            this.gitRepoMetadataProvider = gitRepoMetadataProvider ?? throw new ArgumentNullException(nameof(gitRepoMetadataProvider));
            this.branchConfigurationCalculator = branchConfigurationCalculator ?? throw new ArgumentNullException(nameof(branchConfigurationCalculator));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public GitVersionContext Create(Arguments arguments)
        {
            var targetBranch = repository.GetTargetBranch(arguments.TargetBranch);
            return Init(targetBranch, arguments.CommitId, arguments.OnlyTrackedBranches);
        }

        private GitVersionContext Init(Branch currentBranch, string commitId = null, bool onlyTrackedBranches = false)
        {
            if (currentBranch == null)
                throw new InvalidOperationException("Need a branch to operate on");

            var configuration = configProvider.Provide(overrideConfig: options.Value.OverrideConfig);

            var currentCommit = repository.GetCurrentCommit(log, currentBranch, commitId);

            if (currentBranch.IsDetachedHead())
            {
                var branchForCommit = gitRepoMetadataProvider.GetBranchesContainingCommit(currentCommit, repository.Branches.ToList(), onlyTrackedBranches).OnlyOrDefault();
                currentBranch = branchForCommit ?? currentBranch;
            }

            var currentBranchConfig = branchConfigurationCalculator.GetBranchConfiguration(currentBranch, currentCommit, configuration);
            var effectiveConfiguration = configuration.CalculateEffectiveConfiguration(currentBranchConfig);
            var currentCommitTaggedVersion = repository.GetCurrentCommitTaggedVersion(currentCommit, effectiveConfiguration);

            return new GitVersionContext(currentBranch, currentCommit, configuration, effectiveConfiguration, currentCommitTaggedVersion);
        }
    }
}
