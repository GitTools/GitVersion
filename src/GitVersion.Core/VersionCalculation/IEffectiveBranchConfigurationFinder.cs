using GitVersion.Model.Configurations;

namespace GitVersion.VersionCalculation
{
    public interface IEffectiveBranchConfigurationFinder
    {
        IEnumerable<EffectiveBranchConfiguration> GetConfigurations(IBranch branch, Configuration configuration);
    }
}
