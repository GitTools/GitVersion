namespace GitVersion.VersionCalculation;

public interface IContinuousDeliveryVersionCalculator
{
    SemanticVersion Calculate(NextVersion nextVersion);
}
