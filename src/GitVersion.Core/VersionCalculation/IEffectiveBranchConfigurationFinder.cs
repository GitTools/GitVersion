using GitVersion.Configuration;

namespace GitVersion.VersionCalculation
{
    public interface IEffectiveBranchConfigurationFinder
    {
        IEnumerable<EffectiveBranchConfiguration> GetConfigurations(IBranch branch, IGitVersionConfiguration configuration);
    }
}
