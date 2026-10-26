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

        var targetLabel = configuration.Value.GetBranchSpecificLabel(context.CurrentBranch.Name, null, environment, context.CurrentCommit);
        var iteration = new MainlineIterationBuilder(contextLazy, repositoryStore, taggedSemanticVersionService, environment)
            .BuildIteration(configuration.Branch, targetLabel);
        yield return MainlineVersionCalculator.DetermineBaseVersion(
            iteration, targetLabel, incrementStrategyFinder, context.Configuration, environment, context.CurrentCommit);
    }
}
