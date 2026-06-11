namespace GitVersion;

/// <summary>Controls the leniency applied when parsing semantic version strings.</summary>
public enum SemanticVersionFormat
{
    /// <summary>Parses only fully SemVer 2.0-compliant version strings.</summary>
    Strict,

    /// <summary>Accepts a wider range of version string formats, including four-part and partially-specified versions.</summary>
    Loose
}
