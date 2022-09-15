namespace GitVersion.VersionCalculation;

public interface IBaseVersionCalculator
{
    (SemanticVersion IncrementedVersion, BaseVersion Version) GetBaseVersion();
}
