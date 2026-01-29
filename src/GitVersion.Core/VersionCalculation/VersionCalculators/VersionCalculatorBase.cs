using GitVersion.Common;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

internal abstract class VersionCalculatorBase(
    ILogger logger, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
{
    protected readonly ILogger logger = logger.NotNull();
    protected readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly Lazy<GitVersionContext> versionContext = versionContext.NotNull();

    protected GitVersionContext Context => this.versionContext.Value;

    protected SemanticVersionBuildMetaData CreateVersionBuildMetaData(IBaseVersion baseVersion)
    {
        var commitLogs = this.repositoryStore.GetCommitLog(
            baseVersionSource: baseVersion.BaseVersionSource,
            currentCommit: Context.CurrentCommit,
            ignore: Context.Configuration.Ignore
        );

        var commitsSinceTag = commitLogs.Count;
        this.logger.LogInformation("{CommitsSinceTag} commits found between {BaseVersionSource} and {CurrentCommit}", commitsSinceTag, baseVersion.BaseVersionSource, Context.CurrentCommit);

        var shortSha = Context.CurrentCommit.Id.ToString(7);
        return new SemanticVersionBuildMetaData(
            versionSourceSemVer: baseVersion.SemanticVersion,
            versionSourceSha: baseVersion.BaseVersionSource?.Sha,
            commitsSinceTag: commitsSinceTag,
            branch: Context.CurrentBranch.Name.Friendly,
            commitSha: Context.CurrentCommit.Sha,
            commitShortSha: shortSha,
            commitDate: Context.CurrentCommit.When,
            numberOfUnCommittedChanges: Context.NumberOfUncommittedChanges,
            versionSourceIncrement: baseVersion.Increment
        );
    }
}
