using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation.Caching;

public interface IGitVersionCache
{
    void WriteVariablesToDiskCache(GitVersionCacheKey cacheKey, GitVersionVariables variablesFromCache);
    string GetCacheDirectory();
    GitVersionVariables? LoadVersionVariablesFromDiskCache(GitVersionCacheKey key);
    string GetCacheFileName(GitVersionCacheKey cacheKey);
}
