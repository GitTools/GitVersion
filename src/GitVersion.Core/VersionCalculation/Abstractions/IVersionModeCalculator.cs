namespace GitVersion.VersionCalculation;

public interface IVersionModeCalculator
{
    SemanticVersion Calculate(NextVersion nextVersion);
}
