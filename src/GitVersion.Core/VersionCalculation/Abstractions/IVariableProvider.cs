using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation;

public interface IVariableProvider
{
    VersionVariables GetVariablesFor(NextVersion nextVersion, bool isCurrentCommitTagged);
}
