using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal sealed class EffectiveBranchConfigurationFinder(ILogger<EffectiveBranchConfigurationFinder> logger, IRepositoryStore repositoryStore) : IEffectiveBranchConfigurationFinder
{
    private readonly ILogger<EffectiveBranchConfigurationFinder> logger = logger.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();

    public IEnumerable<EffectiveBranchConfiguration> GetConfigurations(IBranch branch, IGitVersionConfiguration configuration)
    {
        branch.NotNull();
        configuration.NotNull();

        return GetEffectiveConfigurationsRecursive(branch, configuration, null, []);
    }

    private IEnumerable<EffectiveBranchConfiguration> GetEffectiveConfigurationsRecursive(
        IBranch branch, IGitVersionConfiguration configuration, IBranchConfiguration? childBranchConfiguration, HashSet<IBranch> traversedBranches)
    {
        if (!traversedBranches.Add(branch)) yield break; // This should never happen!! But it is good to have a circuit breaker.

        var branchConfiguration = configuration.GetBranchConfiguration(branch.Name);
        if (childBranchConfiguration != null)
        {
            branchConfiguration = childBranchConfiguration.Inherit(branchConfiguration);
        }

        IBranch[] sourceBranches = [];
        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            // At this point we need to check if source branches are available.
            sourceBranches = [.. this.repositoryStore.GetSourceBranches(branch, configuration, traversedBranches)];

            if (sourceBranches.Length == 0)
            {
                // Because the actual branch is marked with the inherit increment strategy we need to either skip the iteration or go further
                // while inheriting from the fallback branch configuration. This behavior is configurable via the increment settings of the configuration.
                var skipTraversingOfOrphanedBranches = configuration.Increment == IncrementStrategy.Inherit;
                this.logger.LogInformation(
                    "An orphaned branch '{Branch}' has been detected and will be skipped={SkipTraversing}.",
                    branch, skipTraversingOfOrphanedBranches
                );
                if (skipTraversingOfOrphanedBranches) yield break;
            }
        }

        if (branchConfiguration.Increment == IncrementStrategy.Inherit && sourceBranches.Length != 0)
        {
            foreach (var sourceBranch in sourceBranches)
            {
                foreach (var effectiveConfiguration
                    in GetEffectiveConfigurationsRecursive(sourceBranch, configuration, branchConfiguration, traversedBranches))
                {
                    yield return effectiveConfiguration;
                }
            }
        }
        else
        {
            yield return new(new(configuration, branchConfiguration), branch);
        }
    }
}
