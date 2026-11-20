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
        => GetEffectiveConfiguration(iteration.Configuration, this.commitResolver.Traverse(iteration));

    public EffectiveConfiguration GetEffectiveConfiguration(MainlineCommit commit)
        => GetEffectiveConfiguration(commit.Configuration, this.commitResolver.Traverse(commit));

    private EffectiveConfiguration GetEffectiveConfiguration(
        IBranchConfiguration branchConfiguration, IEnumerable<MainlineCommit> commits)
    {
        if (branchConfiguration.Increment != IncrementStrategy.Inherit)
        {
            return GetOrCreateConfiguration(branchConfiguration);
        }
        using var iterator = commits.GetEnumerator();
        if (!iterator.MoveNext())
        {
            return GetOrCreateConfiguration(branchConfiguration);
        }
        var firstConfiguration = iterator.Current.Configuration;
        var inheritedConfiguration = ResolveConfiguration(iterator);
        return ReferenceEquals(branchConfiguration, firstConfiguration)
            ? inheritedConfiguration
            : GetInheritedConfiguration(branchConfiguration, inheritedConfiguration);
    }

    private EffectiveConfiguration ResolveConfiguration(IEnumerator<MainlineCommit> commits)
    {
        Stack<MainlineCommit> pending = [];
        EffectiveConfiguration? result;
        IBranchConfiguration previousConfiguration;
        do
        {
            var current = commits.Current;
            previousConfiguration = current.Configuration;
            if (this.commitConfigurations.TryGetValue(current, out result))
            {
                break;
            }
            if (current.Configuration.Increment != IncrementStrategy.Inherit)
            {
                result = GetOrCreateConfiguration(current.Configuration);
                this.commitConfigurations.Add(current, result);
                break;
            }
            pending.Push(current);
        }
        while (commits.MoveNext());

        result ??= GetOrCreateConfiguration(previousConfiguration);
        // Resolve oldest first so each nearer override inherits fully resolved settings.
        while (pending.TryPop(out var current))
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
