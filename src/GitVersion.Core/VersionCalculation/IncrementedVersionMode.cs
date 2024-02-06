namespace GitVersion.VersionCalculation;

public enum TakeIncrementedVersion
{
    TakeAlwaysBaseVersion,
    TakeTaggedOtherwiseIncrementedVersion,
    TakeAlwaysIncrementedVersion
}
