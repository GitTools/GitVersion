using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion;

/// <summary>
/// Contextual information about where GitVersion is being run
/// </summary>
public class GitVersionContext
{
    /// <summary>
    /// Contains the raw configuration, use Configuration for specific configuration based on the current GitVersion context.
    /// </summary>
    public IGitVersionConfiguration Configuration { get; }

    public SemanticVersion? CurrentCommitTaggedVersion { get; }

    public IBranch CurrentBranch { get; }

    public ICommit CurrentCommit { get; }

    public bool IsCurrentCommitTagged => CurrentCommitTaggedVersion != null;

    public int NumberOfUncommittedChanges { get; }

    public GitVersionContext(IBranch currentBranch, ICommit currentCommit,
        IGitVersionConfiguration configuration, SemanticVersion? currentCommitTaggedVersion, int numberOfUncommittedChanges)
    {
        CurrentBranch = currentBranch.NotNull();
        CurrentCommit = currentCommit.NotNull();
        Configuration = configuration.NotNull();
        CurrentCommitTaggedVersion = currentCommitTaggedVersion;
        NumberOfUncommittedChanges = numberOfUncommittedChanges;
    }
}
