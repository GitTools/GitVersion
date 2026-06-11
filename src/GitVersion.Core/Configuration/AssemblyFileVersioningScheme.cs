namespace GitVersion.Configuration;

/// <summary>Determines which version components are included in the assembly file version (<c>AssemblyFileVersionAttribute</c>).</summary>
public enum AssemblyFileVersioningScheme
{
    /// <summary>Uses Major.Minor.Patch.PreReleaseNumber as the assembly file version.</summary>
    MajorMinorPatchTag,

    /// <summary>Uses Major.Minor.Patch.0 as the assembly file version.</summary>
    MajorMinorPatch,

    /// <summary>Uses Major.Minor.0.0 as the assembly file version.</summary>
    MajorMinor,

    /// <summary>Uses Major.0.0.0 as the assembly file version.</summary>
    Major,

    /// <summary>Does not set the assembly file version.</summary>
    None
}
