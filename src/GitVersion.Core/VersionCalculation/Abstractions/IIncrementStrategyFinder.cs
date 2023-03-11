using GitVersion.Configuration;

namespace GitVersion.VersionCalculation;

public interface IIncrementStrategyFinder
{
    VersionField DetermineIncrementedField(GitVersionContext context, BaseVersion baseVersion, EffectiveConfiguration configuration);

    VersionField? GetIncrementForCommits(IGitVersionConfiguration configuration, IEnumerable<ICommit> commits);
}
