using GitVersion.OutputVariables;

namespace GitVersion;

/// <summary>Orchestrates the end-to-end version calculation and returns the resulting version variables.</summary>
public interface IGitVersionCalculateTool
{
    /// <summary>Calculates all version variables for the current repository state.</summary>
    GitVersionVariables CalculateVersionVariables();
}
