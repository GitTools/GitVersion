using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation.Caching;

/// <summary>Persists and retrieves <see cref="GitVersionVariables"/> from a disk cache to avoid redundant recalculation.</summary>
public interface IGitVersionCacheProvider
{
    /// <summary>Writes <paramref name="versionVariables"/> to the on-disk cache.</summary>
    void WriteVariablesToDiskCache(GitVersionVariables versionVariables);

    /// <summary>Loads and returns the cached <see cref="GitVersionVariables"/>, or <see langword="null"/> if the cache is absent or stale.</summary>
    GitVersionVariables? LoadVersionVariablesFromDiskCache();
}
