using GitVersion.OutputVariables;

namespace GitVersion.Cache
{
    public interface IGitVersionCache
    {
        void WriteVariablesToDiskCache(GitVersionCacheKey cacheKey, VersionVariables variablesFromCache);
        string GetCacheDirectory();
        VersionVariables LoadVersionVariablesFromDiskCache(GitVersionCacheKey key);
    }
}
