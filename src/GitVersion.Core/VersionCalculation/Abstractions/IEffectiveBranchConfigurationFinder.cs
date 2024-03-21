using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.VersionCalculation
{
    public interface IEffectiveBranchConfigurationFinder
    {
        IEnumerable<EffectiveBranchConfiguration> GetConfigurations(IBranch branch, IGitVersionConfiguration configuration);
    }
}
