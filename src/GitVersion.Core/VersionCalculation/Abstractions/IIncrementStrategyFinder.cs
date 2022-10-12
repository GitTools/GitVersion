using GitVersion.Model.Configurations;

namespace GitVersion.VersionCalculation;

public interface IIncrementStrategyFinder
{
    VersionField DetermineIncrementedField(GitVersionContext context, BaseVersion baseVersion, EffectiveConfiguration configuration);

    VersionField? GetIncrementForCommits(Model.Configurations.Configuration configuration, IEnumerable<ICommit> commits);
}
