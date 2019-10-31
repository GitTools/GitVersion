using GitVersion.OutputVariables;

namespace GitVersion.Cache
{
    public interface IGitVersionCache
    {
        void WriteVariablesToDiskCache(IGitPreparer gitPreparer, GitVersionCacheKey cacheKey, VersionVariables variablesFromCache);
        string GetCacheDirectory(IGitPreparer gitPreparer);
        VersionVariables LoadVersionVariablesFromDiskCache(IGitPreparer gitPreparer, GitVersionCacheKey key);
    }
}