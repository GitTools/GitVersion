using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline;

// Calculation state belongs to Step 2, not to the graph constructed in Step 1.
internal sealed class MainlineConfigurationResolver(
    IGitVersionConfiguration configuration, IMainlineCommitResolver commitResolver)
{
    private readonly IGitVersionConfiguration configuration = configuration.NotNull();
    private readonly IMainlineCommitResolver commitResolver = commitResolver.NotNull();
    private readonly Dictionary<MainlineCommit, EffectiveConfiguration> commitConfigurations = [];
    private readonly Dictionary<IBranchConfiguration, EffectiveConfiguration> configurations = new(ReferenceEqualityComparer.Instance);
    // The same inherited settings can be used by merges into different receivers.
    private readonly Dictionary<IBranchConfiguration, Dictionary<EffectiveConfiguration, EffectiveConfiguration>> inheritedConfigurations
        = new(ReferenceEqualityComparer.Instance);

    public EffectiveConfiguration GetEffectiveConfiguration(MainlineIteration iteration)
    {
        if (iteration.Configuration.Increment != IncrementStrategy.Inherit)
        {
            return GetOrCreateConfiguration(iteration.Configuration);
        }

        var commit = iteration.Commits.FirstOrDefault();
        if (commit is null && iteration.HasParentIteration)
        {
            commit = this.commitResolver.Resolve(iteration, iteration.BranchedFrom);
        }
        if (commit is null)
        {
            return GetOrCreateConfiguration(iteration.Configuration);
        }

        var inherited = GetEffectiveConfiguration(commit);
        return ReferenceEquals(iteration.Configuration, commit.Configuration)
            ? inherited
            : GetInheritedConfiguration(iteration.Configuration, inherited);
    }

    public EffectiveConfiguration GetEffectiveConfiguration(MainlineCommit commit)
    {
        if (commit.Configuration.Increment != IncrementStrategy.Inherit)
        {
            return GetOrCreateConfiguration(commit.Configuration);
        }
        if (this.commitConfigurations.TryGetValue(commit, out var result))
        {
            return result;
        }

        Stack<MainlineCommit> pending = [];
        var current = commit;
        // Walk A -> B -> C, where A and B inherit and C supplies the increment.
        // A was not cached, so visit it before checking whether its predecessor was resolved.
        do
        {
            if (current.Configuration.Increment != IncrementStrategy.Inherit)
            {
                result = GetOrCreateConfiguration(current.Configuration);
                this.commitConfigurations.Add(current, result);
                break;
            }

            pending.Push(current);
            // A merged branch inherits from its receiving merge, not its original branch point.
            var predecessor = current.Predecessor ?? (current.Iteration.HasParentIteration
                ? this.commitResolver.Resolve(current.Iteration, current.Iteration.BranchedTo)
                : null);
            if (predecessor is null)
            {
                result = GetOrCreateConfiguration(current.Configuration);
                break;
            }
            current = predecessor;
        }
        while (!this.commitConfigurations.TryGetValue(current, out result));

        var previousConfiguration = current.Configuration;
        // Resolve C -> B -> A: B inherits C, then A inherits B while keeping A's overrides.
        // For labels A="nearest", B="older", C="base", cache B="older" and A="nearest".
        while (pending.TryPop(out current))
        {
            if (!ReferenceEquals(current.Configuration, previousConfiguration))
            {
                result = GetInheritedConfiguration(current.Configuration, result);
            }
            this.commitConfigurations.Add(current, result);
            previousConfiguration = current.Configuration;
        }
        return result;
    }

    private EffectiveConfiguration GetInheritedConfiguration(
        IBranchConfiguration branchConfiguration, EffectiveConfiguration inheritedConfiguration)
    {
        if (!this.inheritedConfigurations.TryGetValue(branchConfiguration, out var inherited))
        {
            inherited = [];
            this.inheritedConfigurations.Add(branchConfiguration, inherited);
        }
        if (!inherited.TryGetValue(inheritedConfiguration, out var result))
        {
            var resolvedConfiguration = branchConfiguration.Inherit(inheritedConfiguration);
            result = new EffectiveConfiguration(this.configuration, resolvedConfiguration);
            inherited.Add(inheritedConfiguration, result);
        }
        return result;
    }

    private EffectiveConfiguration GetOrCreateConfiguration(IBranchConfiguration branchConfiguration)
    {
        if (!this.configurations.TryGetValue(branchConfiguration, out var result))
        {
            result = new EffectiveConfiguration(this.configuration, branchConfiguration);
            this.configurations.Add(branchConfiguration, result);
        }
        return result;
    }
}
