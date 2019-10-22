using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IGitVersionComputer
    {
        VersionVariables ComputeVersionVariables(Arguments arguments);
        bool TryGetVersion(string directory, out VersionVariables versionVariables, bool noFetch);
    }
}
