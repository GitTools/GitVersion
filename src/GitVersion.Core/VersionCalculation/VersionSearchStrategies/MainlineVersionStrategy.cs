using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.VersionCalculation;

internal sealed class MainlineVersionStrategy(
    Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore,
    ITaggedSemanticVersionService taggedSemanticVersionService,
    IIncrementStrategyFinder incrementStrategyFinder,
    IEnvironment environment)
    : IVersionStrategy
{
    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();
        var context = contextLazy.Value;
        if (!context.Configuration.VersionStrategy.HasFlag(VersionStrategies.Mainline))
        {
            yield break;
        }

        var targetLabel = configuration.Value.GetBranchSpecificLabel(
            context.CurrentBranch.Name, null, environment, context.CurrentCommit
        );
        MainlineIterationBuilder iterationBuilder = new(
            contextLazy, repositoryStore, taggedSemanticVersionService, environment
        );
        var iteration = iterationBuilder.BuildIteration(configuration.Branch, targetLabel);

        MainlineBranchPointResolver branchPointResolver = new(iteration);
        MainlineConfigurationResolver configurationResolver = new(context.Configuration, branchPointResolver);
        MainlineHistory history = new(
            context.Configuration, environment, context.CurrentCommit, branchPointResolver, configurationResolver
        );
        MainlineVersionCalculator versionCalculator = new(
            incrementStrategyFinder, branchPointResolver, configurationResolver, history
        );
        yield return versionCalculator.DetermineBaseVersion(iteration, targetLabel);
    }
}
