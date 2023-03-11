using GitVersion.Configuration;

namespace GitVersion;

public enum IncrementStrategy
{
    None,
    Major,
    Minor,
    Patch,
    /// <summary>
    /// Uses the <see cref="IBranchConfiguration.Increment"/>, <see cref="IBranchConfiguration.PreventIncrementOfMergedBranchVersion"/> and <see cref="IBranchConfiguration.TracksReleaseBranches"/>
    /// of the "parent" branch (i.e. the branch where the current branch was branched from).
    /// </summary>
    Inherit
}

public static class IncrementStrategyExtensions
{
    public static VersionField ToVersionField(this IncrementStrategy strategy) => strategy switch
    {
        IncrementStrategy.None => VersionField.None,
        IncrementStrategy.Major => VersionField.Major,
        IncrementStrategy.Minor => VersionField.Minor,
        IncrementStrategy.Patch => VersionField.Patch,
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
    };
}
