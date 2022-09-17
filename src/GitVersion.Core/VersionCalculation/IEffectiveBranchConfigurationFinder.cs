using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation
{
    public interface IEffectiveBranchConfigurationFinder
    {
        IEnumerable<(IBranch Branch, EffectiveConfiguration Configuration)> GetConfigurations(IBranch branch, Config configuration);
    }
}
