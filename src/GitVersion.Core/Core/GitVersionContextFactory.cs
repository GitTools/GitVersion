using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using Microsoft.Extensions.Options;

namespace GitVersion;

internal class GitVersionContextFactory : IGitVersionContextFactory
{
    private readonly IConfigurationProvider configurationProvider;
    private readonly IRepositoryStore repositoryStore;
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository;
    private readonly IOptions<GitVersionOptions> options;

    public GitVersionContextFactory(IConfigurationProvider configurationProvider, IRepositoryStore repositoryStore,
        ITaggedSemanticVersionRepository taggedSemanticVersionRepository, IOptions<GitVersionOptions> options)
    {
        this.configurationProvider = configurationProvider.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();
        this.options = options.NotNull();
    }

    public GitVersionContext Create(GitVersionOptions gitVersionOptions)
    {
        var overrideConfiguration = this.options.Value.ConfigurationInfo.OverrideConfiguration;
        var configuration = this.configurationProvider.Provide(overrideConfiguration);

        var currentBranch = this.repositoryStore.GetTargetBranch(gitVersionOptions.RepositoryInfo.TargetBranch)
            ?? throw new InvalidOperationException("Need a branch to operate on");
        var currentCommit = this.repositoryStore.GetCurrentCommit(
            currentBranch, gitVersionOptions.RepositoryInfo.CommitId, configuration.Ignore
        );

        if (currentCommit is null) throw new GitVersionException("No commits found on the current branch.");

        if (currentBranch.IsDetachedHead)
        {
            var branchForCommit = this.repositoryStore.GetBranchesContainingCommit(
                currentCommit, onlyTrackedBranches: gitVersionOptions.Settings.OnlyTrackedBranches
            ).OnlyOrDefault();
            currentBranch = branchForCommit ?? currentBranch;
        }

        bool isCurrentCommitTagged = this.taggedSemanticVersionRepository.GetTaggedSemanticVersions(
            tagPrefix: configuration.TagPrefix,
            format: configuration.SemanticVersionFormat,
            ignore: configuration.Ignore
        ).Contains(currentCommit);
        var numberOfUncommittedChanges = this.repositoryStore.GetNumberOfUncommittedChanges();

        return new(currentBranch, currentCommit, configuration, isCurrentCommitTagged, numberOfUncommittedChanges);
    }
}
