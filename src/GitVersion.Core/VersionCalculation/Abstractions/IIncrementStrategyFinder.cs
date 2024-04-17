using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

public interface IIncrementStrategyFinder
{
    VersionField DetermineIncrementedField(
        ICommit currentCommit, ICommit? baseVersionSource, bool shouldIncrement, EffectiveConfiguration configuration, string? label);

    VersionField? GetIncrementForCommits(
        string? majorVersionBumpMessage, string? minorVersionBumpMessage, string? patchVersionBumpMessage, string? noBumpMessage,
        ICommit[] commits
    );

    IEnumerable<ICommit> GetMergedCommits(ICommit mergeCommit, int index, IIgnoreConfiguration ignore);

    VersionField GetIncrementForcedByCommit(ICommit commit, IGitVersionConfiguration configuration);
}
