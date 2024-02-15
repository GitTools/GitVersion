using GitVersion.Configuration;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation;

public interface IVariableProvider
{
    GitVersionVariables GetVariablesFor(
        SemanticVersion semanticVersion, IGitVersionConfiguration configuration, int preReleaseWeight);
}
