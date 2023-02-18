using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.Caching;

public interface IGitVersionCacheKeyFactory
{
    GitVersionCacheKey Create(GitVersionConfiguration? overrideConfiguration);
}
