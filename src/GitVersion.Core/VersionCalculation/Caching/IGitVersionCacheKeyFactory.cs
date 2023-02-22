namespace GitVersion.VersionCalculation.Caching;

public interface IGitVersionCacheKeyFactory
{
    GitVersionCacheKey Create(IReadOnlyDictionary<object, object?>? overrideConfiguration);
}
