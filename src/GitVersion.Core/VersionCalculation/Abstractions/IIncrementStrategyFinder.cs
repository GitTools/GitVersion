using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

public interface IIncrementStrategyFinder
{
    VersionField DetermineIncrementedField(
        ICommit currentCommit, ICommit? baseVersionSource, bool shouldIncrement, EffectiveConfiguration configuration, string? label);

    IEnumerable<ICommit> GetMergedCommits(ICommit mergeCommit, int index, IIgnoreConfiguration ignore);

    VersionField GetIncrementForcedByCommit(ICommit commit, IGitVersionConfiguration configuration);
}
