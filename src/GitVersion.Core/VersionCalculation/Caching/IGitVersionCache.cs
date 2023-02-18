using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation.Caching;

public interface IGitVersionCache
{
    void WriteVariablesToDiskCache(GitVersionCacheKey cacheKey, VersionVariables variablesFromCache);
    string GetCacheDirectory();
    VersionVariables? LoadVersionVariablesFromDiskCache(GitVersionCacheKey key);
}
