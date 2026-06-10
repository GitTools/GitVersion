namespace GitVersion.VersionCalculation;

/// <summary>Applies a deployment-mode-specific calculation to transform a base semantic version into the final version.</summary>
public interface IDeploymentModeCalculator
{
    /// <summary>Calculates the final <see cref="SemanticVersion"/> by applying deployment-mode rules to <paramref name="semanticVersion"/> and <paramref name="baseVersion"/>.</summary>
    SemanticVersion Calculate(SemanticVersion semanticVersion, IBaseVersion baseVersion);
}
