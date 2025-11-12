using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Git;
using Microsoft.Extensions.Logging;

namespace GitVersion.VersionCalculation;

internal abstract class VersionCalculatorBase<T>(
    ILogger<T> logger, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
{
    protected readonly ILogger<T> logger = logger.NotNull();
    protected readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly Lazy<GitVersionContext> versionContext = versionContext.NotNull();

    protected GitVersionContext Context => this.versionContext.Value;

    protected SemanticVersionBuildMetaData CreateVersionBuildMetaData(ICommit? baseVersionSource)
    {
        var commitLogs = this.repositoryStore.GetCommitLog(
            baseVersionSource: baseVersionSource,
            currentCommit: Context.CurrentCommit,
            ignore: Context.Configuration.Ignore
        );

        var commitsSinceTag = commitLogs.Count;
        this.logger.LogInformation("{CommitsSinceTag} commits found between {BaseVersionSource} and {CurrentCommit}",
            commitsSinceTag, baseVersionSource, Context.CurrentCommit);

        var shortSha = Context.CurrentCommit.Id.ToString(7);
        return new SemanticVersionBuildMetaData(
            versionSourceSha: baseVersionSource?.Sha,
            commitsSinceTag: commitsSinceTag,
            branch: Context.CurrentBranch.Name.Friendly,
            commitSha: Context.CurrentCommit.Sha,
            commitShortSha: shortSha,
            commitDate: Context.CurrentCommit.When,
            numberOfUnCommittedChanges: Context.NumberOfUncommittedChanges
        );
    }
}
