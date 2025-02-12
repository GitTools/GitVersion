namespace GitVersion.Configuration;

public interface IPreventIncrementConfiguration
{
    bool? OfMergedBranch { get; }

    bool? WhenBranchMerged { get; }

    bool? WhenCurrentCommitTagged { get; }
}
