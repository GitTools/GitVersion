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
    private readonly Dictionary<MainlineIteration, EffectiveConfiguration> iterationConfigurations = [];
    private readonly Dictionary<IBranchConfiguration, EffectiveConfiguration> configurations = new(ReferenceEqualityComparer.Instance);
    // An inherited branch configuration is reusable only with equivalent receiving settings.
    private readonly Dictionary<IBranchConfiguration, Dictionary<EffectiveConfiguration, EffectiveConfiguration>> inheritedConfigurations
        = new(ReferenceEqualityComparer.Instance);

    public EffectiveConfiguration GetEffectiveConfiguration(MainlineCommit commit)
    {
        if (this.commitConfigurations.TryGetValue(commit, out var cached))
        {
            return cached;
        }
        if (commit.Configuration.Increment != IncrementStrategy.Inherit)
        {
            return GetOrCreateConfiguration(commit.Configuration);
        }
        return ResolveInheritedConfiguration(commit);
    }

    public EffectiveConfiguration GetEffectiveConfiguration(MainlineIteration iteration)
    {
        if (this.iterationConfigurations.TryGetValue(iteration, out var cached))
        {
            return cached;
        }

        var branchConfiguration = iteration.Configuration;
        EffectiveConfiguration result;
        MainlineCommit? commit = null;
        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            commit = iteration.Commits.FirstOrDefault();
            if (commit is null && iteration.HasParentIteration)
            {
                commit = this.commitResolver.Resolve(iteration, iteration.BranchedFrom);
            }
        }
        if (commit is not null)
        {
            var inheritedConfiguration = GetEffectiveConfiguration(commit);
            result = ReferenceEquals(branchConfiguration, commit.Configuration)
                ? inheritedConfiguration
                : GetInheritedConfiguration(branchConfiguration, inheritedConfiguration);
        }
        else
        {
            result = GetOrCreateConfiguration(branchConfiguration);
        }
        this.iterationConfigurations.Add(iteration, result);
        return result;
    }

    private EffectiveConfiguration ResolveInheritedConfiguration(MainlineCommit commit)
    {
        List<MainlineCommit> pending = [];
        var current = commit;
        EffectiveConfiguration result;
        while (true)
        {
            if (this.commitConfigurations.TryGetValue(current, out var currentConfiguration))
            {
                result = currentConfiguration;
                break;
            }
            if (current.Configuration.Increment != IncrementStrategy.Inherit)
            {
                result = GetOrCreateConfiguration(current.Configuration);
                this.commitConfigurations.Add(current, result);
                break;
            }
            pending.Add(current);
            if (current.Predecessor is { } predecessor)
            {
                current = predecessor;
                continue;
            }
            if (current.Iteration.HasParentIteration)
            {
                var receivingMerge = current.Iteration.GetBranchedTo().NotNull();
                result = GetEffectiveConfiguration(receivingMerge);
                result = GetInheritedConfiguration(current.Configuration, result);
            }
            else
            {
                result = GetOrCreateConfiguration(current.Configuration);
            }
            this.commitConfigurations.Add(current, result);
            pending.RemoveAt(pending.Count - 1);
            break;
        }

        var previousConfiguration = current.Configuration;
        for (var index = pending.Count - 1; index >= 0; index--)
        {
            current = pending[index];
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
