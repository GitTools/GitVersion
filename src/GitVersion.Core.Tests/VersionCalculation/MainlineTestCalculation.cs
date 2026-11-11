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
    {
        MainlineCommitResolver commitResolver = new(iteration);
        MainlineConfigurationResolver configurationResolver = new(configuration, commitResolver);
        MainlineHistory history = new(configuration, environment, currentCommit, commitResolver, configurationResolver);
        return new(incrementStrategyFinder ?? Substitute.For<IIncrementStrategyFinder>(),
            commitResolver, configurationResolver, history);
    }

    internal static MainlineContext Context(
        MainlineIteration iteration, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit)
        => Calculator(iteration, configuration, environment, currentCommit).CreateContext();

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
