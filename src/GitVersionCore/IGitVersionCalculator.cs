using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IGitVersionCalculator
    {
        VersionVariables CalculateVersionVariables(Arguments arguments);
        bool TryCalculateVersionVariables(string directory, bool noFetch, out VersionVariables versionVariables);
    }
}
