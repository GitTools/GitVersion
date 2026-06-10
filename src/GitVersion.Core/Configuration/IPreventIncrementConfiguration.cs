namespace GitVersion.Configuration;

/// <summary>Controls under what circumstances automatic version increments should be suppressed.</summary>
public interface IPreventIncrementConfiguration
{
    /// <summary>Gets a value indicating whether version increment is prevented when this branch is itself the result of a merge.</summary>
    bool? OfMergedBranch { get; }

    /// <summary>Gets a value indicating whether version increment is prevented at the point when this branch is merged into another.</summary>
    bool? WhenBranchMerged { get; }

    /// <summary>Gets a value indicating whether version increment is prevented when the current commit is already tagged.</summary>
    bool? WhenCurrentCommitTagged { get; }
}
