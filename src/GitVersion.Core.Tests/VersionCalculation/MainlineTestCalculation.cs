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
        MainlineBranchPointResolver branchPointResolver = new(iteration);
        MainlineConfigurationResolver configurationResolver = new(configuration, branchPointResolver);
        MainlineHistory history = new(configuration, environment, currentCommit, branchPointResolver, configurationResolver);
        return new(incrementStrategyFinder ?? Substitute.For<IIncrementStrategyFinder>(),
            branchPointResolver, configurationResolver, history);
    }

    internal static MainlineContext Context(
        MainlineIteration iteration, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit)
        => Calculator(iteration, configuration, environment, currentCommit).CreateContext();

    internal static MainlineHistory History(
        MainlineIteration iteration, IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit,
        MainlineBranchPointResolver? branchPointResolver = null)
    {
        branchPointResolver ??= new MainlineBranchPointResolver(iteration);
        MainlineConfigurationResolver configurationResolver = new(configuration, branchPointResolver);
        return new(configuration, environment, currentCommit, branchPointResolver, configurationResolver);
    }
}
