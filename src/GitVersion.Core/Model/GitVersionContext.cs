using GitVersion.Configuration;
using GitVersion.Model.Configuration;

namespace GitVersion;

/// <summary>
/// Contextual information about where GitVersion is being run
/// </summary>
public class GitVersionContext
{
    /// <summary>
    /// Contains the raw configuration, use Configuration for specific config based on the current GitVersion context.
    /// </summary>
    public Config FullConfiguration { get; }

    public SemanticVersion? CurrentCommitTaggedVersion { get; }

    public IBranch CurrentBranch { get; }

    public ICommit? CurrentCommit { get; }

    public bool IsCurrentCommitTagged => CurrentCommitTaggedVersion != null;

    public int NumberOfUncommittedChanges { get; }

    public GitVersionContext(IBranch currentBranch, ICommit? currentCommit,
        Config configuration, SemanticVersion? currentCommitTaggedVersion, int numberOfUncommittedChanges)
    {
        CurrentBranch = currentBranch;
        CurrentCommit = currentCommit;
        FullConfiguration = configuration;
        CurrentCommitTaggedVersion = currentCommitTaggedVersion;
        NumberOfUncommittedChanges = numberOfUncommittedChanges;
    }

    public EffectiveConfiguration GetEffectiveConfiguration(IBranch branch)
    {
        BranchConfig branchConfiguration = FullConfiguration.GetBranchConfiguration(branch);
        return new EffectiveConfiguration(FullConfiguration, branchConfiguration);
    }
}
