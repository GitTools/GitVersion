using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation.Caching;

public interface IGitVersionCache
{
    void WriteVariablesToDiskCache(GitVersionCacheKey cacheKey, GitVersionVariables variablesFromCache);
    GitVersionVariables? LoadVersionVariablesFromDiskCache(GitVersionCacheKey cacheKey);
}
