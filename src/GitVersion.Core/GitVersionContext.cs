using GitVersion.Configuration;

namespace GitVersion;

/// <summary>
/// Contextual information about where GitVersion is being run
/// </summary>
public class GitVersionContext
{
    /// <summary>
    /// Contains the raw configuration, use Configuration for specific configuration based on the current GitVersion context.
    /// </summary>
    public GitVersionConfiguration Configuration { get; }

    public SemanticVersion? CurrentCommitTaggedVersion { get; }

    public IBranch CurrentBranch { get; }

    public ICommit? CurrentCommit { get; }

    public bool IsCurrentCommitTagged => CurrentCommitTaggedVersion != null;

    public int NumberOfUncommittedChanges { get; }

    public GitVersionContext(IBranch currentBranch, ICommit? currentCommit,
        GitVersionConfiguration configuration, SemanticVersion? currentCommitTaggedVersion, int numberOfUncommittedChanges)
    {
        CurrentBranch = currentBranch;
        CurrentCommit = currentCommit;
        Configuration = configuration;
        CurrentCommitTaggedVersion = currentCommitTaggedVersion;
        NumberOfUncommittedChanges = numberOfUncommittedChanges;
    }

    public EffectiveConfiguration GetEffectiveConfiguration(IBranch branch)
    {
        BranchConfiguration branchConfiguration = Configuration.GetBranchConfiguration(branch);
        return new EffectiveConfiguration(Configuration, branchConfiguration);
    }
}
