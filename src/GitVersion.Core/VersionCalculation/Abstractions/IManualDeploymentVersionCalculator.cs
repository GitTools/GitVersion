namespace GitVersion.VersionCalculation;

public interface IManualDeploymentVersionCalculator
{
    SemanticVersion Calculate(NextVersion nextVersion);
}
