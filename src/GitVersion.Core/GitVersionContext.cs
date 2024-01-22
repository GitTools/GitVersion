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
    SemanticVersion? currentCommitTaggedVersion,
    int numberOfUncommittedChanges)
{
    /// <summary>
    /// Contains the raw configuration, use Configuration for specific configuration based on the current GitVersion context.
    /// </summary>
    public IGitVersionConfiguration Configuration { get; } = configuration.NotNull();

    public SemanticVersion? CurrentCommitTaggedVersion { get; } = currentCommitTaggedVersion;

    public IBranch CurrentBranch { get; } = currentBranch.NotNull();

    public ICommit CurrentCommit { get; } = currentCommit.NotNull();

    public bool IsCurrentCommitTagged => CurrentCommitTaggedVersion != null;

    public int NumberOfUncommittedChanges { get; } = numberOfUncommittedChanges;
}
