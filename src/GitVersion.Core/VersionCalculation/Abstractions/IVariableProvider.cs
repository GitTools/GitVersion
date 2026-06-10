using GitVersion.Configuration;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation;

/// <summary>Converts a calculated <see cref="SemanticVersion"/> into the full set of <see cref="GitVersionVariables"/>.</summary>
public interface IVariableProvider
{
    /// <summary>Builds and returns all version variables for the given <paramref name="semanticVersion"/>.</summary>
    GitVersionVariables GetVariablesFor(
        SemanticVersion semanticVersion, IGitVersionConfiguration configuration, int preReleaseWeight);
}
