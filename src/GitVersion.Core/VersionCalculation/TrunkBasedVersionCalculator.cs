namespace GitVersion.VersionCalculation;

internal sealed class TrunkBasedVersionCalculator : IVersionModeCalculator
{
    // TODO: Please implement trunk based version here and remove MainlineVersionCalculator.
    public SemanticVersion Calculate(NextVersion nextVersion) =>
        throw new NotImplementedException("Trunk based version calculation is not yet implemented. Use Mainline mode instead.");
}
