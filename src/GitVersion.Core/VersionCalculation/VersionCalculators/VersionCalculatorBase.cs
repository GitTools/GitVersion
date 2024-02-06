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

    protected bool ShouldTakeIncrementedVersion(NextVersion nextVersion)
    {
        nextVersion.NotNull();

        bool shouldTakeIncrementedVersion = Context.CurrentCommit.Sha != nextVersion.BaseVersion.BaseVersionSource?.Sha
            || Context.CurrentCommitTaggedVersion is null
            || nextVersion.BaseVersion.SemanticVersion != Context.CurrentCommitTaggedVersion;

        return nextVersion.Configuration.TakeIncrementedVersion == TakeIncrementedVersion.TakeAlwaysIncrementedVersion
            || nextVersion.Configuration.TakeIncrementedVersion == TakeIncrementedVersion.TakeTaggedOtherwiseIncrementedVersion && shouldTakeIncrementedVersion;
    }

    protected SemanticVersion CalculateIncrementedVersion(NextVersion nextVersion)
    {
        ////
        // TODO: We need to consider somehow the IGitVersionConfiguration::Ignore property here!!
        var semanticVersionWithTag = this.repositoryStore.GetTaggedSemanticVersionsOnBranch(
            nextVersion.BranchConfiguration.Branch, Context.Configuration.TagPrefix, Context.Configuration.SemanticVersionFormat
        ).FirstOrDefault(element => Context.CurrentCommit is null || element.Tag.Commit.When <= Context.CurrentCommit.When);
        //

        if (semanticVersionWithTag?.Value.CompareTo(nextVersion.IncrementedVersion, false) > 0)
        {
            return new(semanticVersionWithTag.Value)
            {
                PreReleaseTag = new(nextVersion.IncrementedVersion.PreReleaseTag),
                BuildMetaData = CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource)
            };
        }

        return new(nextVersion.IncrementedVersion)
        {
            BuildMetaData = CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource)
        };
    }

    protected SemanticVersionBuildMetaData CreateVersionBuildMetaData(ICommit? baseVersionSource)
    {
        int commitsSinceTag = 0;
        if (Context.CurrentCommit != null)
        {
            var commitLogs = this.repositoryStore.GetCommitLog(baseVersionSource, Context.CurrentCommit);

            var ignore = Context.Configuration.Ignore;
            if (!ignore.IsEmpty)
            {
                var shasToIgnore = new HashSet<string>(ignore.Shas);
                commitLogs = commitLogs
                    .Where(c => ignore.Before is null || (c.When > ignore.Before && !shasToIgnore.Contains(c.Sha)));
            }
            commitsSinceTag = commitLogs.Count();

            this.log.Info($"{commitsSinceTag} commits found between {baseVersionSource} and {Context.CurrentCommit}");
        }

        var shortSha = Context.CurrentCommit?.Id.ToString(7);
        return new(
            versionSourceSha: baseVersionSource?.Sha,
            commitsSinceTag: commitsSinceTag,
            branch: Context.CurrentBranch.Name.Friendly,
            commitSha: Context.CurrentCommit?.Sha,
            commitShortSha: shortSha,
            commitDate: Context.CurrentCommit?.When,
            numberOfUnCommittedChanges: Context.NumberOfUncommittedChanges
        );
    }
}
