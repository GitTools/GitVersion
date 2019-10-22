using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IGitVersionComputer
    {
        VersionVariables ComputeVersionVariables(Arguments arguments);
        bool TryGetVersion(string directory, bool noFetch, out VersionVariables versionVariables);
    }
}
