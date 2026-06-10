namespace GitVersion;

/// <summary>Identifies the position in a semantic version string that should be incremented.</summary>
public enum VersionField
{
    /// <summary>No field is incremented; the pre-release number is bumped instead.</summary>
    None,

    /// <summary>Increment the patch component.</summary>
    Patch,

    /// <summary>Increment the minor component (and reset patch to zero).</summary>
    Minor,

    /// <summary>Increment the major component (and reset minor and patch to zero).</summary>
    Major
}
