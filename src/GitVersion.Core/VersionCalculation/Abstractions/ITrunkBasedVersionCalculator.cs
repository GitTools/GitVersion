namespace GitVersion.VersionCalculation;

public interface ITrunkBasedVersionCalculator
{
    SemanticVersion Calculate(NextVersion nextVersion);
}
