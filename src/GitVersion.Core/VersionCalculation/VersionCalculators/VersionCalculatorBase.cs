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
        var semVerSource = baseVersion switch
        {
            ResolvedBaseVersion resolved => resolved.SemVerSource,
            BaseVersion candidate => candidate.GetSemVerSource(),
            _ => new SemanticVersionSource(baseVersion.Source, baseVersion.SemanticVersion, baseVersion.BaseVersionSource, baseVersion.Increment)
        };
        var commitLogs = this.repositoryStore.GetCommitLog(
            baseVersionSource: baseVersion.BaseVersionSource,
            currentCommit: Context.CurrentCommit,
            ignore: Context.Configuration.Ignore
        );

        var commitCount = commitLogs.Count;
        this.logger.LogInformation("{CommitCount} commits found between {CommitCountSource} and {CurrentCommit}", commitCount, baseVersion.BaseVersionSource, Context.CurrentCommit);

        var shortSha = Context.CurrentCommit.Id.ToString(7);
        return new SemanticVersionBuildMetaData(
            versionSourceSemVer: baseVersion.SemanticVersion,
            versionSourceSha: baseVersion.BaseVersionSource?.Sha,
            commitsSinceTag: commitCount,
            branch: Context.CurrentBranch.Name.Friendly,
            commitSha: Context.CurrentCommit.Sha,
            commitShortSha: shortSha,
            commitDate: Context.CurrentCommit.When,
            numberOfUnCommittedChanges: Context.NumberOfUncommittedChanges,
            versionSourceIncrement: baseVersion.Increment
        )
        {
            SemVerSourceSemVer = semVerSource.Version,
            SemVerSourceSha = semVerSource.Commit?.Sha,
            SemVerSourceIncrement = semVerSource.Increment
        };
    }
}
