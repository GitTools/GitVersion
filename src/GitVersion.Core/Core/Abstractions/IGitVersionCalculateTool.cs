using GitVersion.OutputVariables;

namespace GitVersion;

public interface IGitVersionCalculateTool
{
    VersionVariables CalculateVersionVariables();
}
