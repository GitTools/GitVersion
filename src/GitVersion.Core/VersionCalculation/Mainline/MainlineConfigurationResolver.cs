using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline;

// Calculation state belongs to Step 2, not to the graph constructed in Step 1.
internal sealed class MainlineConfigurationResolver(
    IGitVersionConfiguration configuration, MainlineBranchPointResolver branchPointResolver)
{
    private readonly IGitVersionConfiguration configuration = configuration.NotNull();
    private readonly MainlineBranchPointResolver branchPointResolver = branchPointResolver.NotNull();
    private readonly Dictionary<MainlineCommit, EffectiveConfiguration> commitConfigurations = [];
    private readonly Dictionary<MainlineIteration, EffectiveConfiguration> iterationConfigurations = [];

    public EffectiveConfiguration GetEffectiveConfiguration(MainlineCommit commit)
    {
        if (this.commitConfigurations.TryGetValue(commit, out var cached))
        {
            return cached;
        }

        var branchConfiguration = commit.Configuration;
        var last = branchConfiguration;
        for (var current = commit; current is not null; current = current.Predecessor)
        {
            if (branchConfiguration.Increment != IncrementStrategy.Inherit)
            {
                break;
            }
            if (current.Configuration != last)
            {
                branchConfiguration = branchConfiguration.Inherit(current.Configuration);
            }
            last = current.Configuration;
        }

        if (branchConfiguration.Increment == IncrementStrategy.Inherit && commit.Iteration.HasParentIteration)
        {
            var receivingMerge = commit.Iteration.GetBranchedTo().NotNull();
            branchConfiguration = branchConfiguration.Inherit(GetEffectiveConfiguration(receivingMerge));
        }

        var result = new EffectiveConfiguration(this.configuration, branchConfiguration);
        this.commitConfigurations.Add(commit, result);
        return result;
    }

    public EffectiveConfiguration GetEffectiveConfiguration(MainlineIteration iteration)
    {
        if (this.iterationConfigurations.TryGetValue(iteration, out var cached))
        {
            return cached;
        }

        var branchConfiguration = iteration.Configuration;
        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            var commit = iteration.Commits.FirstOrDefault()
                ?? (iteration.HasParentIteration ? this.branchPointResolver.Resolve(iteration) : null);
            if (commit is not null)
            {
                branchConfiguration = branchConfiguration.Inherit(GetEffectiveConfiguration(commit));
            }
        }

        var result = new EffectiveConfiguration(this.configuration, branchConfiguration);
        this.iterationConfigurations.Add(iteration, result);
        return result;
    }

    // Release selection uses recorded settings and global defaults, without ancestry inheritance.
    public EffectiveConfiguration GetOwnConfiguration(MainlineCommit commit)
        => new(this.configuration, commit.Configuration);

    public bool IsMergedMainline(MainlineCommit commit)
        => commit.ChildIteration is { } child && GetEffectiveConfiguration(child).IsMainBranch
            && GetEffectiveConfiguration(commit).IsMainBranch;

    public VersionField GetIncrementForcedByBranch(MainlineCommit commit)
        => GetEffectiveConfiguration(commit).Increment.ToVersionField();
}
