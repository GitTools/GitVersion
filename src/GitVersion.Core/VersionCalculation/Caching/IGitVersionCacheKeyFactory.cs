namespace GitVersion.VersionCalculation.Caching;

internal interface IGitVersionCacheKeyFactory
{
    GitVersionCacheKey Create(IReadOnlyDictionary<object, object?>? overrideConfiguration);
}
