using GitVersion.Configuration;

namespace GitVersion.OutputVariables
{
    public interface IVariableProvider
    {
        VersionVariables GetVariablesFor(SemanticVersion semanticVersion, EffectiveConfiguration config, bool isCurrentCommitTagged);
    }
}
