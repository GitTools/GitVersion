using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

/// <summary>Determines the version-field increment that should be applied based on commit messages and branch context.</summary>
public interface IIncrementStrategyFinder
{
    /// <summary>Determines which version field to increment given the current commit, base version source, and branch configuration.</summary>
    VersionField DetermineIncrementedField(
        ICommit currentCommit, ICommit? baseVersionSource, bool shouldIncrement, EffectiveConfiguration configuration, string? label);

    /// <summary>Returns the commits that were merged as part of a merge commit at the given <paramref name="index"/>.</summary>
    IEnumerable<ICommit> GetMergedCommits(ICommit mergeCommit, int index, IIgnoreConfiguration ignore);

    /// <summary>Returns the version field increment forced by a commit message keyword in <paramref name="commit"/>.</summary>
    VersionField GetIncrementForcedByCommit(ICommit commit, IGitVersionConfiguration configuration);
}
