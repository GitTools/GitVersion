using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IGitVersionCalculator
    {
        VersionVariables CalculateVersionVariables();
    }
}
