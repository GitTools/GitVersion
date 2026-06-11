namespace GitVersion;

/// <summary>Specifies which version component should be incremented when creating a release from a branch.</summary>
public enum IncrementStrategy
{
    /// <summary>No automatic increment is applied.</summary>
    None,

    /// <summary>Increment the major component.</summary>
    Major,

    /// <summary>Increment the minor component.</summary>
    Minor,

    /// <summary>Increment the patch component.</summary>
    Patch,

    /// <summary>Inherit the increment strategy from the parent or source branch.</summary>
    Inherit
}
