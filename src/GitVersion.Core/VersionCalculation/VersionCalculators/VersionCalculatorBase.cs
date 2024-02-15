using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal abstract class VersionCalculatorBase(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
{
    protected readonly ILog log = log.NotNull();
    protected readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly Lazy<GitVersionContext> versionContext = versionContext.NotNull();

    protected GitVersionContext Context => this.versionContext.Value;

    protected SemanticVersionBuildMetaData CreateVersionBuildMetaData(ICommit? baseVersionSource)
    {
        int commitsSinceTag = 0;
        var commitLogs = this.repositoryStore.GetCommitLog(baseVersionSource, Context.CurrentCommit);

        var ignore = Context.Configuration.Ignore;
        if (!ignore.IsEmpty)
        {
            commitLogs = commitLogs
                .Where(c => ignore.Before is null || (c.When > ignore.Before && !ignore.Shas.Contains(c.Sha)));
        }
        commitsSinceTag = commitLogs.Count();

        this.log.Info($"{commitsSinceTag} commits found between {baseVersionSource} and {Context.CurrentCommit}");

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
