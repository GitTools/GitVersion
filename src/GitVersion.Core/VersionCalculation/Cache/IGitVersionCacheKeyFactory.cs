using GitVersion.Cache;

namespace GitVersion.VersionCalculation.Cache;

public interface IGitVersionCacheKeyFactory
{
    GitVersionCacheKey Create(Model.Configurations.Configuration? overrideConfig);
}
