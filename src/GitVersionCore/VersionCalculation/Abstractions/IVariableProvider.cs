using GitVersion.Model.Configuration;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation
{
    public interface IVariableProvider
    {
        VersionVariables GetVariablesFor(SemanticVersion semanticVersion, EffectiveConfiguration config, bool isCurrentCommitTagged);
    }
}
