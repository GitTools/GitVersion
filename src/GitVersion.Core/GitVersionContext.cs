using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

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

    /// <summary>Gets the branch currently being versioned.</summary>
    public IBranch CurrentBranch { get; } = currentBranch.NotNull();

    /// <summary>Gets the commits on the current branch that were authored before the current commit.</summary>
    public IEnumerable<ICommit> CurrentBranchCommits => CurrentBranch.Commits.GetCommitsPriorTo(CurrentCommit.When);

    /// <summary>Gets the commit being versioned.</summary>
    public ICommit CurrentCommit { get; } = currentCommit.NotNull();

    /// <summary>Gets a value indicating whether the current commit has an exact-match version tag.</summary>
    public bool IsCurrentCommitTagged { get; } = isCurrentCommitTagged;

    /// <summary>Gets the number of files that have been modified but not yet committed.</summary>
    public int NumberOfUncommittedChanges { get; } = numberOfUncommittedChanges;
}
