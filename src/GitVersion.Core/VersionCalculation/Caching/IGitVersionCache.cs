using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation.Caching;

public interface IGitVersionCache
{
    void WriteVariablesToDiskCache(GitVersionCacheKey cacheKey, GitVersionVariables versionVariables);
    GitVersionVariables? LoadVersionVariablesFromDiskCache(GitVersionCacheKey cacheKey);
}
