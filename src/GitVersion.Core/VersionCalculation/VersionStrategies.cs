namespace GitVersion.VersionCalculation;

/// <summary>A bitmask that enables or disables individual version-discovery strategies.</summary>
[Flags]
public enum VersionStrategies
{
    /// <summary>No strategies are enabled.</summary>
    None = 0,

    /// <summary>Uses a fallback version (typically <c>0.1.0</c>) when no other strategy finds a version.</summary>
    Fallback = 1,

    /// <summary>Uses the <c>next-version</c> value from the configuration file.</summary>
    ConfiguredNextVersion = 2,

    /// <summary>Extracts the version from merge commit messages.</summary>
    MergeMessage = 4,

    /// <summary>Uses the most recent version tag reachable from the current commit.</summary>
    TaggedCommit = 8,

    /// <summary>Tracks the version from related release branches.</summary>
    TrackReleaseBranches = 16,

    /// <summary>Extracts the version embedded in the branch name.</summary>
    VersionInBranchName = 32,

    /// <summary>Uses the mainline development strategy to calculate the version from the commit graph.</summary>
    Mainline = 64
}
