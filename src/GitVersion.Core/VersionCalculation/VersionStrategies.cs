namespace GitVersion.VersionCalculation;

[Flags]
public enum VersionStrategies
{
    None = 0,
    Fallback = 1,
    ConfiguredNextVersion = 2,
    MergeMessage = 4,
    TaggedCommit = 8,
    TrackReleaseBranches = 16,
    VersionInBranchName = 32,
    Mainline = 64,
    MergeCommit = 128
}
