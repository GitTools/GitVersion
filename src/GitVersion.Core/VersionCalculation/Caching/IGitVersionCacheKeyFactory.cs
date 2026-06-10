namespace GitVersion.VersionCalculation.Caching;

/// <summary>Represents the cache key used to identify a stored set of <see cref="GitVersion.OutputVariables.GitVersionVariables"/> on disk.</summary>
public record GitVersionCacheKey(string Value);

internal interface IGitVersionCacheKeyFactory
{
    GitVersionCacheKey Create(IReadOnlyDictionary<object, object?>? overrideConfiguration);
}
