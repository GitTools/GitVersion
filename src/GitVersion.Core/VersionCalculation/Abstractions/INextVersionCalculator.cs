namespace GitVersion.VersionCalculation;

/// <summary>Orchestrates the full version calculation pipeline and returns the next semantic version.</summary>
public interface INextVersionCalculator
{
    /// <summary>Calculates and returns the next semantic version for the current repository state.</summary>
    SemanticVersion FindVersion();
}
