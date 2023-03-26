using GitVersion.Configuration;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation;

public interface IVariableProvider
{
    GitVersionVariables GetVariablesFor(
        SemanticVersion semanticVersion, EffectiveConfiguration configuration, SemanticVersion? currentCommitTaggedVersion
    );
}
