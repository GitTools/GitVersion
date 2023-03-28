using GitVersion.OutputVariables;

namespace GitVersion;

public interface IGitVersionCalculateTool
{
    GitVersionVariables CalculateVersionVariables();
}
