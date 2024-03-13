using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion;

/// <summary>
/// Contextual information about where GitVersion is being run
/// </summary>
public class GitVersionContext(
    IBranch currentBranch,
    ICommit currentCommit,
    IGitVersionConfiguration configuration,
    bool isCurrentCommitTagged,
    int numberOfUncommittedChanges)
{
    /// <summary>
    /// Contains the raw configuration, use Configuration for specific configuration based on the current GitVersion context.
    /// </summary>
    public IGitVersionConfiguration Configuration { get; } = configuration.NotNull();

    public IBranch CurrentBranch { get; } = currentBranch.NotNull();

    public IEnumerable<ICommit> CurrentBranchCommits => CurrentBranch.Commits.GetCommitsPriorTo(CurrentCommit.When);

    public ICommit CurrentCommit { get; } = currentCommit.NotNull();

    public bool IsCurrentCommitTagged { get; } = isCurrentCommitTagged;

    public int NumberOfUncommittedChanges { get; } = numberOfUncommittedChanges;
}
