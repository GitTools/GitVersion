using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

internal sealed class EffectiveBranchConfigurationFinder : IEffectiveBranchConfigurationFinder
{
    private readonly ILog log;
    private readonly IRepositoryStore repositoryStore;

    public EffectiveBranchConfigurationFinder(ILog log, IRepositoryStore repositoryStore)
    {
        this.log = log.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
    }

    public IEnumerable<(IBranch Branch, EffectiveConfiguration Configuration)> GetConfigurations(IBranch branch, Config configuration)
    {
        branch.NotNull();
        configuration.NotNull();
        return GetEffectiveConfigurationsRecursive(branch, configuration, null, new());
    }

    private IEnumerable<(IBranch Branch, EffectiveConfiguration Configuration)> GetEffectiveConfigurationsRecursive(IBranch branch, Config configuration,
        BranchConfig? childBranchConfiguration, HashSet<IBranch> traversedBranches)
    {
        if (!traversedBranches.Add(branch)) yield break;

        var branchConfiguration = configuration.GetBranchConfiguration(branch.Name.WithoutRemote);
        if (childBranchConfiguration != null)
        {
            branchConfiguration = childBranchConfiguration.Inherit(branchConfiguration);
        }

        var targetBranches = Array.Empty<IBranch>();
        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            targetBranches = repositoryStore.GetTargetBranches(branch, configuration, traversedBranches).ToArray();

            if (targetBranches.Length == 0)
            {
                var fallbackBranchConfiguration = configuration.GetFallbackBranchConfiguration();
                if (fallbackBranchConfiguration.Increment == IncrementStrategy.Inherit)
                {
                    fallbackBranchConfiguration.Increment = IncrementStrategy.None;
                }
                branchConfiguration = branchConfiguration.Inherit(fallbackBranchConfiguration);
            }
        }

        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            foreach (var targetBranche in targetBranches)
            {
                foreach (var effectiveConfiguration
                    in GetEffectiveConfigurationsRecursive(targetBranche, configuration, branchConfiguration, traversedBranches))
                {
                    yield return effectiveConfiguration;
                }
            }
        }
        else
        {
            yield return new(branch, new EffectiveConfiguration(configuration, branchConfiguration));
        }
    }
}
