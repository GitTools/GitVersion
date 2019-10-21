using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IExecuteCore
    {
        VersionVariables ExecuteGitVersion(Arguments arguments);
        bool TryGetVersion(string directory, out VersionVariables versionVariables, bool noFetch, Authentication authentication);
    }
}