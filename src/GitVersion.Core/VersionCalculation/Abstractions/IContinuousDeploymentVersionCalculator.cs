namespace GitVersion.VersionCalculation;

public interface IContinuousDeploymentVersionCalculator
{
    SemanticVersion Calculate(NextVersion nextVersion);
}
