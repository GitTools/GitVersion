using GitVersion.Configuration;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation;

public interface IVariableProvider
{
    VersionVariables GetVariablesFor(SemanticVersion semanticVersion, EffectiveConfiguration configuration, bool isCurrentCommitTagged);
}
