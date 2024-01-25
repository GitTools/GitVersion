namespace GitVersion.VersionCalculation;

[Flags]
public enum VersionStrategies
{
    None = 0,
    ConfigNext = 1,
    MergeMessage = 2,
    TaggedCommit = 4,
    TrackReleaseBranches = 8,
    VersionInBranchName = 16,
    TrunkBased = 32,
}
