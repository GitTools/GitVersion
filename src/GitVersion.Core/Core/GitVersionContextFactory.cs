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
        var currentBranch = this.repositoryStore.GetTargetBranch(gitVersionOptions.RepositoryInfo.TargetBranch) ?? throw new InvalidOperationException("Need a branch to operate on");
        var currentCommit = this.repositoryStore.GetCurrentCommit(currentBranch, gitVersionOptions.RepositoryInfo.CommitId);

        var overrideConfiguration = this.options.Value.ConfigurationInfo.OverrideConfiguration;
        var configuration = this.configurationProvider.Provide(overrideConfiguration);
        if (currentBranch.IsDetachedHead)
        {
            var branchForCommit = this.repositoryStore.GetBranchesContainingCommit(currentCommit, onlyTrackedBranches: gitVersionOptions.Settings.OnlyTrackedBranches).OnlyOrDefault();
            currentBranch = branchForCommit ?? currentBranch;
        }

        if (currentBranch.IsRemote)
        {
            var remoteNameInGit = configuration.RemoteNameInGit;
            if (string.IsNullOrEmpty(remoteNameInGit) || !currentBranch.Name.Friendly.StartsWith(remoteNameInGit))
            {
                throw new InvalidOperationException(
                    $"The remote branch name '{currentBranch.Name.Friendly}' is not valid. Please use another branch or change the configuration."
                );
            }
        }

        var currentCommitTaggedVersion = this.repositoryStore.GetCurrentCommitTaggedVersion(currentCommit, configuration.LabelPrefix, configuration.SemanticVersionFormat, handleDetachedBranch: currentBranch.IsDetachedHead);
        var numberOfUncommittedChanges = this.repositoryStore.GetNumberOfUncommittedChanges();

        return new GitVersionContext(currentBranch, currentCommit, configuration, currentCommitTaggedVersion, numberOfUncommittedChanges);
    }
}
