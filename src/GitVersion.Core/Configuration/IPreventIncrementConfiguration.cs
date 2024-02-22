namespace GitVersion.Configuration;

public interface IPreventIncrementConfiguration
{
    public bool? OfMergedBranch { get; }

    public bool? WhenBranchMerged { get; }

    public bool? WhenCurrentCommitTagged { get; }
}
