using GitVersion.Configuration;

namespace GitVersion.VersionCalculation;

public interface IVersionStrategy
{
    IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration);
}
