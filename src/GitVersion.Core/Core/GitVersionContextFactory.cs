using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion;

internal class GitVersionContextFactory(
    Lazy<IGitVersionConfiguration> configuration,
    IRepositoryStore repositoryStore,
    ITaggedSemanticVersionRepository taggedSemanticVersionRepository,
    IOptions<GitVersionOptions> options)
    : IGitVersionContextFactory
{
    private readonly Lazy<IGitVersionConfiguration> configuration = configuration.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();
    private readonly IOptions<GitVersionOptions> options = options.NotNull();

    public GitVersionContext Create()
    {
        var gitVersionOptions = this.options.Value;
        var effectiveConfiguration = this.configuration.Value;

        var currentBranch = this.repositoryStore.GetTargetBranch(gitVersionOptions.RepositoryInfo.TargetBranch)
            ?? throw new InvalidOperationException("Need a branch to operate on");
        var currentCommit = this.repositoryStore.GetCurrentCommit(
            currentBranch, gitVersionOptions.RepositoryInfo.CommitId, effectiveConfiguration.Ignore
        ) ?? throw new GitVersionException("No commits found on the current branch.");
        if (currentBranch.IsDetachedHead)
        {
            var branchForCommit = this.repositoryStore.GetBranchesContainingCommit(
                currentCommit, onlyTrackedBranches: gitVersionOptions.Settings.OnlyTrackedBranches
            ).OnlyOrDefault();
            currentBranch = branchForCommit ?? currentBranch;
        }

        var isCurrentCommitTagged = this.taggedSemanticVersionRepository.GetTaggedSemanticVersions(
            tagPrefix: effectiveConfiguration.TagPrefixPattern,
            format: effectiveConfiguration.SemanticVersionFormat,
            ignore: effectiveConfiguration.Ignore
        ).Contains(currentCommit);
        var numberOfUncommittedChanges = this.repositoryStore.UncommittedChangesCount;

        return new(currentBranch, currentCommit, effectiveConfiguration, isCurrentCommitTagged, numberOfUncommittedChanges);
    }
}
