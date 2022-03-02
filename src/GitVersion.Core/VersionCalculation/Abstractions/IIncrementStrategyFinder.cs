namespace GitVersion.VersionCalculation;

public interface IIncrementStrategyFinder
{
    VersionField? DetermineIncrementedField(IGitRepository repository, GitVersionContext context, BaseVersion baseVersion);
    VersionField? GetIncrementForCommits(GitVersionContext context, IEnumerable<ICommit> commits);
}
