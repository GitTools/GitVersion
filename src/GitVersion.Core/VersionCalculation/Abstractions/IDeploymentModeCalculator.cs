namespace GitVersion.VersionCalculation;

public interface IDeploymentModeCalculator
{
    SemanticVersion Calculate(SemanticVersion semanticVersion, IBaseVersion baseVersion);
}
