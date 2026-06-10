namespace GitVersion.VersionCalculation;

/// <summary>Controls whether and how commit messages are used to determine automatic version increments.</summary>
public enum CommitMessageIncrementMode
{
    /// <summary>All commit messages are inspected for increment keywords.</summary>
    Enabled,

    /// <summary>Commit messages are never inspected; increments must be configured explicitly.</summary>
    Disabled,

    /// <summary>Only merge commit messages are inspected for increment keywords.</summary>
    MergeMessageOnly
}
