using GitVersion.Common;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal abstract class NonTrunkBasedVersionCalculatorBase
{
    protected readonly ILog log;
    protected readonly IRepositoryStore repositoryStore;
    private readonly Lazy<GitVersionContext> versionContext;

    protected GitVersionContext Context => this.versionContext.Value;

    public NonTrunkBasedVersionCalculatorBase(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext)
    {
        this.log = log.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.versionContext = versionContext.NotNull();
    }

    protected bool ShouldTakeIncrementedVersion(NextVersion nextVersion)
    {
        nextVersion.NotNull();

        // TODO: We need to decide whether or not to calculate the upcoming semantic version or the previous one because of tagging. Actually this should be part of the branch configuration system.
        return Context.CurrentCommit.Sha != nextVersion.BaseVersion.BaseVersionSource?.Sha
            || Context.CurrentCommitTaggedVersion is null
            || nextVersion.BaseVersion.SemanticVersion != Context.CurrentCommitTaggedVersion;
        //
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
            return new SemanticVersion(semanticVersionWithTag.Value)
            {
                PreReleaseTag = new SemanticVersionPreReleaseTag(nextVersion.IncrementedVersion.PreReleaseTag),
                BuildMetaData = CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource)
            };
        }

        return new SemanticVersion(nextVersion.IncrementedVersion)
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
        return new SemanticVersionBuildMetaData(
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
