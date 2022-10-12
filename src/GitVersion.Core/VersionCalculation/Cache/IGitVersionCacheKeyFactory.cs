using GitVersion.Cache;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation.Cache;

public interface IGitVersionCacheKeyFactory
{
    GitVersionCacheKey Create(GitVersionConfiguration? overrideConfiguration);
}
