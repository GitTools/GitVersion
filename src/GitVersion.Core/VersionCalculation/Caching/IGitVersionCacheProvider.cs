using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation.Caching;

public interface IGitVersionCacheProvider
{
    void WriteVariablesToDiskCache(GitVersionVariables versionVariables);
    GitVersionVariables? LoadVersionVariablesFromDiskCache();
}
