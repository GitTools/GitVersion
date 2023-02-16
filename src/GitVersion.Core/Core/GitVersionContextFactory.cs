using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitVersionContextFactory : IGitVersionContextFactory
{
    private readonly IConfigurationProvider configurationProvider;
    private readonly IRepositoryStore repositoryStore;
    private readonly IOptions<GitVersionOptions> options;

    public GitVersionContextFactory(IConfigurationProvider configurationProvider, IRepositoryStore repositoryStore, IOptions<GitVersionOptions> options)
    {
        this.configurationProvider = configurationProvider.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.options = options.NotNull();
    }

    public GitVersionContext Create(GitVersionOptions gitVersionOptions)
    {
        var currentBranch = this.repositoryStore.GetTargetBranch(gitVersionOptions.RepositoryInfo.TargetBranch);
        if (currentBranch == null)
            throw new InvalidOperationException("Need a branch to operate on");

        var currentCommit = this.repositoryStore.GetCurrentCommit(currentBranch, gitVersionOptions.RepositoryInfo.CommitId);

        var configuration = this.configurationProvider.Provide(this.options.Value.ConfigInfo.OverrideConfig);
        if (currentBranch.IsDetachedHead)
        {
            var branchForCommit = this.repositoryStore.GetBranchesContainingCommit(currentCommit, onlyTrackedBranches: gitVersionOptions.Settings.OnlyTrackedBranches).OnlyOrDefault();
            currentBranch = branchForCommit ?? currentBranch;
        }

        var currentCommitTaggedVersion = this.repositoryStore.GetCurrentCommitTaggedVersion(currentCommit, configuration.LabelPrefix, handleDetachedBranch: currentBranch.IsDetachedHead);
        var numberOfUncommittedChanges = this.repositoryStore.GetNumberOfUncommittedChanges();

        return new GitVersionContext(currentBranch, currentCommit, configuration, currentCommitTaggedVersion, numberOfUncommittedChanges);
    }
}
