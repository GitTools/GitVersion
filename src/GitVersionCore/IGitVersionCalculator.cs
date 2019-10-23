using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IGitVersionCalculator
    {
        VersionVariables CalculateVersionVariables(Arguments arguments);
        bool TryCalculateVersionVariables(Arguments arguments, out VersionVariables versionVariables);
    }
}
