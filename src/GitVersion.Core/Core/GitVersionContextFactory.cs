using GitVersion.BuildAgents;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitVersionContextFactory : IGitVersionContextFactory
{
    private readonly IConfigProvider configProvider;
    private readonly IRepositoryStore repositoryStore;
    private readonly IBranchConfigurationCalculator branchConfigurationCalculator;
    private readonly IBuildAgent buildAgent;
    private readonly ILog log;
    private readonly IOptions<GitVersionOptions> options;

    public GitVersionContextFactory(IConfigProvider configProvider, IRepositoryStore repositoryStore, IBranchConfigurationCalculator branchConfigurationCalculator, IBuildAgentResolver buildAgentResolver, ILog log, IOptions<GitVersionOptions> options)
    {
        this.configProvider = configProvider.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.branchConfigurationCalculator = branchConfigurationCalculator.NotNull();
        this.buildAgent = buildAgentResolver.NotNull().Resolve();
        this.log = log.NotNull();
        this.options = options.NotNull();
    }

    public GitVersionContext Create(GitVersionOptions gitVersionOptions)
    {
        var branchCommit = ResolveCurrentBranchCommit(gitVersionOptions);
        var currentBranch = branchCommit.Branch;
        var currentCommit = branchCommit.Commit;
        var configuration = this.configProvider.Provide(this.options.Value.ConfigInfo.OverrideConfig);
        var currentBranchConfig = this.branchConfigurationCalculator.GetBranchConfiguration(currentBranch, currentCommit, configuration);
        var effectiveConfiguration = configuration.CalculateEffectiveConfiguration(currentBranchConfig);
        var currentCommitTaggedVersion = this.repositoryStore.GetCurrentCommitTaggedVersion(currentCommit, effectiveConfiguration);
        var numberOfUncommittedChanges = this.repositoryStore.GetNumberOfUncommittedChanges();

        return new GitVersionContext(currentBranch, currentCommit, configuration, effectiveConfiguration, currentCommitTaggedVersion, numberOfUncommittedChanges);
    }

    private BranchCommit ResolveCurrentBranchCommit(GitVersionOptions gitVersionOptions)
    {
        var currentBranch = ResolveCurrentBranch();
        var currentCommit = this.repositoryStore.GetCurrentCommit(currentBranch, gitVersionOptions.RepositoryInfo.CommitId);

        if (currentBranch.IsDetachedHead)
        {
            currentBranch = this.repositoryStore.GetBranchesContainingCommit(currentCommit,
                onlyTrackedBranches: gitVersionOptions.Settings.OnlyTrackedBranches).OnlyOrDefault()
                            ?? currentBranch;
        }

        return new BranchCommit(currentCommit, currentBranch);
    }

    private IBranch ResolveCurrentBranch()
    {
        var currentBranchName = ResolveCurrentBranchName();
        var currentBranch = this.repositoryStore.GetTargetBranch(currentBranchName);

        if (currentBranch == null)
            throw new InvalidOperationException("Need a branch to operate on");

        return currentBranch;
    }

    private string? ResolveCurrentBranchName()
    {
        var gitVersionOptions = this.options.Value;
        var isDynamicRepository = !gitVersionOptions.RepositoryInfo.DynamicRepositoryClonePath.IsNullOrWhiteSpace();
        var currentBranch = this.buildAgent.GetCurrentBranch(isDynamicRepository);

        if (!currentBranch.IsNullOrWhiteSpace())
        {
            this.log.Info($"Branch from build environment: {currentBranch}");
            return currentBranch;
        }

        var targetBranch = gitVersionOptions.RepositoryInfo.TargetBranch;

        if (!targetBranch.IsNullOrWhiteSpace())
        {
            this.log.Info($"Branch from Git repository: {targetBranch}");
            return targetBranch;
        }

        this.log.Info($"No branch found in environment or repository; this is probably a detached HEAD.");
        return null;
    }
}
