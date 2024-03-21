namespace GitVersion.VersionCalculation.Caching;

public record GitVersionCacheKey(string Value);
internal interface IGitVersionCacheKeyFactory
{
    GitVersionCacheKey Create(IReadOnlyDictionary<object, object?>? overrideConfiguration);
}
