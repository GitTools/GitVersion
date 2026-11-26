using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Mainline;

namespace GitVersion.Tests.VersionCalculation;

internal static class MainlineTestCalculation
{
    internal static MainlineVersionCalculator Calculator(
        MainlineIteration iteration, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit,
        IIncrementStrategyFinder? incrementStrategyFinder = null)
        => CreateCalculator(iteration, configuration, environment, currentCommit, incrementStrategyFinder).Calculator;

    private static (MainlineVersionCalculator Calculator, MainlineCommitResolver CommitResolver,
        MainlineConfigurationResolver ConfigurationResolver, MainlineHistory History) CreateCalculator(
        MainlineIteration iteration, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit,
        IIncrementStrategyFinder? incrementStrategyFinder = null)
    {
        MainlineCommitResolver commitResolver = new(iteration);
        MainlineConfigurationResolver configurationResolver = new(configuration, commitResolver);
        MainlineHistory history = new(configuration, environment, currentCommit, commitResolver, configurationResolver);
        MainlineVersionCalculator calculator = new(incrementStrategyFinder ?? Substitute.For<IIncrementStrategyFinder>(),
            configurationResolver, history);
        return (calculator, commitResolver, configurationResolver, history);
    }

    internal static Components Create(
        MainlineIteration iteration, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit)
    {
        var (calculator, commitResolver, configurationResolver, history) =
            CreateCalculator(iteration, configuration, environment, currentCommit);
        var context = calculator.CreateContext();
        if (commitResolver.Resolve(iteration, currentCommit) is { } commit)
        {
            new ConfigurationPreEnricher(configurationResolver).Enrich(iteration, commit, context);
        }
        return new(calculator, configurationResolver, history, new(configurationResolver, environment, calculator), context);
    }

    internal static MainlineContext Context(
        MainlineIteration iteration, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit)
        => Create(iteration, configuration, environment, currentCommit).CalculationContext;

    internal sealed record Components(MainlineVersionCalculator VersionCalculator, MainlineConfigurationResolver ConfigurationResolver,
        MainlineHistory CommitHistory, MergedMainlineVersionPolicy SourceVersions, MainlineContext CalculationContext);

    internal static MainlineCommit[] SelectHistory(MainlineIteration root, MainlineIteration iteration)
        => History(root, GitFlowConfigurationBuilder.New.Build(), new TestEnvironment(),
            Substitute.For<ICommit>()).Select(iteration, "", true).Commits;

    internal static MainlineHistory History(
        MainlineIteration iteration, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit,
        IMainlineCommitResolver? commitResolver = null)
    {
        commitResolver ??= new MainlineCommitResolver(iteration);
        MainlineConfigurationResolver configurationResolver = new(configuration, commitResolver);
        return new(configuration, environment, currentCommit, commitResolver, configurationResolver);
    }
}
