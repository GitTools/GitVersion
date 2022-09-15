using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitVersionContextFactory : IGitVersionContextFactory
{
    private readonly IConfigProvider configProvider;
    private readonly IRepositoryStore repositoryStore;
    private readonly IBranchConfigurationCalculator branchConfigurationCalculator;
    private readonly IOptions<GitVersionOptions> options;

    public GitVersionContextFactory(IConfigProvider configProvider, IRepositoryStore repositoryStore, IBranchConfigurationCalculator branchConfigurationCalculator, IOptions<GitVersionOptions> options)
    {
        this.configProvider = configProvider.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.branchConfigurationCalculator = branchConfigurationCalculator.NotNull();
        this.options = options.NotNull();
    }

    public GitVersionContext Create(GitVersionOptions gitVersionOptions)
    {
        var currentBranch = this.repositoryStore.GetTargetBranch(gitVersionOptions.RepositoryInfo.TargetBranch);
        if (currentBranch == null)
            throw new InvalidOperationException("Need a branch to operate on");

        var currentCommit = this.repositoryStore.GetCurrentCommit(currentBranch, gitVersionOptions.RepositoryInfo.CommitId);

        var configuration = this.configProvider.Provide(this.options.Value.ConfigInfo.OverrideConfig);
        if (currentBranch.IsDetachedHead)
        {
            var branchForCommit = this.repositoryStore.GetBranchesContainingCommit(currentCommit, onlyTrackedBranches: gitVersionOptions.Settings.OnlyTrackedBranches).OnlyOrDefault();
            currentBranch = branchForCommit ?? currentBranch;
        }

        var currentCommitTaggedVersion = this.repositoryStore.GetCurrentCommitTaggedVersion(currentCommit, configuration.TagPrefix);
        var numberOfUncommittedChanges = this.repositoryStore.GetNumberOfUncommittedChanges();

        var context = new GitVersionContext(currentBranch, currentCommit, configuration, currentCommitTaggedVersion, numberOfUncommittedChanges);

        ////
        // this logic has been moved from GitVersionContextFactory to this class. The reason is that we need to set the
        // effective configuration dynamic dependent on the version strategy implementation. Therefor we are traversing recursive
        // from the current branch to the base branch until the branch configuration indicates no inheritance. Probably we can
        // refactor the hole logic here and remove the complexity.
        var currentBranchConfig = this.branchConfigurationCalculator.GetBranchConfiguration(
            context.CurrentBranch, context.CurrentCommit, context.FullConfiguration);
        context.Configuration = new EffectiveConfiguration(context.FullConfiguration, currentBranchConfig);
        //

        return context;

    }
}
