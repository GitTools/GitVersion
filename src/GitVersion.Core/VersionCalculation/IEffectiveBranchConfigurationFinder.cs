using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation
{
    public interface IEffectiveBranchConfigurationFinder
    {
        IEnumerable<EffectiveBranchConfiguration> GetConfigurations(IBranch branch, GitVersionConfiguration configuration);
    }
}
