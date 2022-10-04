using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

public interface IIncrementStrategyFinder
{
    VersionField DetermineIncrementedField(GitVersionContext context, BaseVersion baseVersion, EffectiveConfiguration configuration);

    VersionField? GetIncrementForCommits(Config configuration, IEnumerable<ICommit> commits);
}
