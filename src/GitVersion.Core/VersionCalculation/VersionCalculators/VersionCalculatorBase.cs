using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal abstract class VersionCalculatorBase(
    ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
{
    protected readonly ILog log = log.NotNull();
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
        this.log.Info($"{commitsSinceTag} commits found between {baseVersion.BaseVersionSource} and {Context.CurrentCommit}");

        var shortSha = Context.CurrentCommit.Id.ToString(7);
        return new SemanticVersionBuildMetaData(
            versionSourceSemVer: baseVersion.SemanticVersion,
            versionSourceSha: baseVersion.BaseVersionSource?.Sha,
            commitsSinceTag: commitsSinceTag,
            branch: Context.CurrentBranch.Name.Friendly,
            commitSha: Context.CurrentCommit.Sha,
            commitShortSha: shortSha,
            commitDate: Context.CurrentCommit.When,
            numberOfUnCommittedChanges: Context.NumberOfUncommittedChanges
        );
    }
}
