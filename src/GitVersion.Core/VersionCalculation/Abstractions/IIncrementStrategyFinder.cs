using GitVersion.Configuration;

namespace GitVersion.VersionCalculation;

public interface IIncrementStrategyFinder
{
    VersionField DetermineIncrementedField(ICommit currentCommit, BaseVersion baseVersion, EffectiveConfiguration configuration);

    VersionField? GetIncrementForCommits(
        string? majorVersionBumpMessage, string? minorVersionBumpMessage, string? patchVersionBumpMessage, string? noBumpMessage,
        ICommit[] commits
    );

    IEnumerable<ICommit> GetMergedCommits(ICommit mergeCommit, int index);

    VersionField GetIncrementForcedByCommit(ICommit commit, EffectiveConfiguration configuration);
}
