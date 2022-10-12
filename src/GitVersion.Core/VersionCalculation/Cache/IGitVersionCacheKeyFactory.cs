using GitVersion.Cache;
using GitVersion.Configuration;

namespace GitVersion.VersionCalculation.Cache;

public interface IGitVersionCacheKeyFactory
{
    GitVersionCacheKey Create(GitVersionConfiguration? overrideConfiguration);
}
