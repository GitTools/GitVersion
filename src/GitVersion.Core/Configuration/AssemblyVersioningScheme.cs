namespace GitVersion.Configuration;

/// <summary>Determines which version components are included in the assembly version (<c>AssemblyVersionAttribute</c>).</summary>
public enum AssemblyVersioningScheme
{
    /// <summary>Uses Major.Minor.Patch.PreReleaseNumber as the assembly version.</summary>
    MajorMinorPatchTag,

    /// <summary>Uses Major.Minor.Patch.0 as the assembly version.</summary>
    MajorMinorPatch,

    /// <summary>Uses Major.Minor.0.0 as the assembly version.</summary>
    MajorMinor,

    /// <summary>Uses Major.0.0.0 as the assembly version.</summary>
    Major,

    /// <summary>Does not set the assembly version.</summary>
    None
}
