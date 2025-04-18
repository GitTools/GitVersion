using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using Microsoft.Extensions.Options;

namespace GitVersion;

internal class GitVersionContextFactory(
    IConfigurationProvider configurationProvider,
    IRepositoryStore repositoryStore,
    ITaggedSemanticVersionRepository taggedSemanticVersionRepository,
    IOptions<GitVersionOptions> options)
    : IGitVersionContextFactory
{
    private readonly IConfigurationProvider configurationProvider = configurationProvider.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    public GitVersionContext Create(GitVersionOptions gitVersionOptions)
    {
        var overrideConfiguration = this.options.Value.ConfigurationInfo.OverrideConfiguration;
        var configuration = this.configurationProvider.Provide(overrideConfiguration);

        var currentBranch = this.repositoryStore.GetTargetBranch(gitVersionOptions.RepositoryInfo.TargetBranch)
            ?? throw new InvalidOperationException("Need a branch to operate on");
        var currentCommit = this.repositoryStore.GetCurrentCommit(
            currentBranch, gitVersionOptions.RepositoryInfo.CommitId, configuration.Ignore
        ) ?? throw new GitVersionException("No commits found on the current branch.");
        if (currentBranch.IsDetachedHead)
        {
            var branchForCommit = this.repositoryStore.GetBranchesContainingCommit(
                currentCommit, onlyTrackedBranches: gitVersionOptions.Settings.OnlyTrackedBranches
            ).OnlyOrDefault();
            currentBranch = branchForCommit ?? currentBranch;
        }

        var isCurrentCommitTagged = this.taggedSemanticVersionRepository.GetTaggedSemanticVersions(
            tagPrefix: configuration.TagPrefixPattern,
            format: configuration.SemanticVersionFormat,
            ignore: configuration.Ignore
        ).Contains(currentCommit);
        var numberOfUncommittedChanges = this.repositoryStore.UncommittedChangesCount;

        return new(currentBranch, currentCommit, configuration, isCurrentCommitTagged, numberOfUncommittedChanges);
    }
}
