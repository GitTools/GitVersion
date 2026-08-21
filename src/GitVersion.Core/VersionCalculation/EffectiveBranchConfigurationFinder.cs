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

        return GetEffectiveConfigurationsRecursive(branch, configuration, null, [], resolvePullRequestTarget: true);
    }

    private IEnumerable<EffectiveBranchConfiguration> GetEffectiveConfigurationsRecursive(
        IBranch branch, IGitVersionConfiguration configuration, IBranchConfiguration? childBranchConfiguration,
        HashSet<IBranch> traversedBranches, bool resolvePullRequestTarget)
    {
        if (!traversedBranches.Add(branch))
        {
            yield break; // This should never happen!! But it is good to have a circuit breaker.
        }

        var branchConfiguration = configuration.GetBranchConfiguration(branch.Name);
        if (childBranchConfiguration != null)
        {
            branchConfiguration = childBranchConfiguration.Inherit(branchConfiguration);
        }

        if (branchConfiguration.Increment != IncrementStrategy.Inherit)
        {
            yield return new(new(configuration, branchConfiguration), branch);
            yield break;
        }

        // At this point we need to check if source branches are available.
        IBranch[] sourceBranches = [.. this.repositoryStore.GetSourceBranches(branch, configuration, traversedBranches)];
        if (resolvePullRequestTarget && GetPullRequestTargetBranch(branch, configuration) is { } targetBranch)
        {
            sourceBranches = [targetBranch];
        }

        if (sourceBranches.Length == 0)
        {
            // Because the actual branch is marked with the inherit increment strategy we need to either skip the iteration or go further
            // while inheriting from the fallback branch configuration. This behavior is configurable via the increment settings of the configuration.
            var skipTraversingOfOrphanedBranches = configuration.Increment == IncrementStrategy.Inherit;
            this.logger.LogInformation(
                "An orphaned branch '{Branch}' has been detected and will be skipped={SkipTraversing}.",
                branch, skipTraversingOfOrphanedBranches
            );
            if (!skipTraversingOfOrphanedBranches)
            {
                yield return new(new(configuration, branchConfiguration), branch);
            }
            yield break;
        }

        foreach (var sourceBranch in sourceBranches)
        {
            foreach (var effectiveConfiguration
                in GetEffectiveConfigurationsRecursive(
                    sourceBranch, configuration, branchConfiguration,
                    new(traversedBranches, traversedBranches.Comparer), resolvePullRequestTarget: false))
            {
                yield return effectiveConfiguration;
            }
        }
    }

    private IBranch? GetPullRequestTargetBranch(IBranch branch, IGitVersionConfiguration configuration)
    {
        if (!IsPullRequestBranch(branch, configuration)
            || branch.Tip is not { } tip
            || !MergeMessage.TryParse(tip, configuration, out var mergeMessage)
            || !mergeMessage.IsMergedPullRequest
            || string.IsNullOrWhiteSpace(mergeMessage.TargetBranch))
        {
            return null;
        }

        var targetBranch = new SourceBranchFinder(this.repositoryStore.Branches, configuration, excludeIgnoredBranches: false)
            .FindSourceBranchesOf(branch)
            .Where(candidate => candidate.Name.EquivalentTo(mergeMessage.TargetBranch))
            .MinBy(candidate => candidate.IsRemote);

        if (targetBranch is null)
        {
            this.logger.LogDebug(
                "Pull request target branch '{TargetBranch}' was not found among the allowed source branches for '{Branch}'.",
                mergeMessage.TargetBranch, branch
            );
        }
        else
        {
            this.logger.LogDebug(
                "Using pull request target branch '{TargetBranch}' as the source branch for '{Branch}'.",
                targetBranch, branch
            );
        }

        return targetBranch;
    }

    private static bool IsPullRequestBranch(IBranch branch, IGitVersionConfiguration configuration) =>
        branch.Name.IsPullRequest
        || configuration.Branches.TryGetValue(ConfigurationConstants.PullRequestBranchKey, out var pullRequestConfiguration)
        && pullRequestConfiguration.IsMatch(branch.Name.WithoutOrigin);
}
