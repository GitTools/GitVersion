using Path = Cake.Core.IO.Path;

namespace Common.Addins.Cake.DotNetCoreFormat;

public sealed class DotNetCoreFormatSettings : DotNetCoreSettings
{
    /// <summary>
    /// A path to a solution file, a project file, or a folder containing a solution or project file.
    /// If a path is not specified then the current directory is used.
    /// </summary>
    public Path? Workspace { get; set; }

    /// <summary>
    /// Run formatting command (Whitespace, style or analyzers). Run by default when not applying fixes.
    /// </summary>
    public DotNetFormatFix Fix { get; set; }

    /// <summary>
    /// A list of diagnostic ids to use as a filter when fixing code style or 3rd party analyzers.
    /// </summary>
    public List<string>? Diagnostics { get; set; }

    /// <summary>
    /// A list of relative file or folder paths to include in formatting. All files are formatted if empty
    /// </summary>
    public List<string>? Include { get; set; }

    /// <summary>
    /// A list of relative file or folder paths to exclude from formatting.
    /// </summary>
    public List<string>? Exclude { get; set; }

    /// <summary>
    /// Formats files without saving changes to disk. Terminates with a non-zero exit code if any files were formatted.
    /// </summary>
    public bool VerifyNoChanges { get; set; }

    /// <summary>
    /// Set the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]
    /// </summary>
    public new DotNetFormatVerbosity Verbosity { get; set; } = DotNetFormatVerbosity.Normal;
}

public enum DotNetFormatFix
{
    Whitespace,
    Style,
    Analyzers
}
/// <summary>
/// Represents verbosity.
/// </summary>
public enum DotNetFormatVerbosity
{
    /// <summary>
    /// Quiet verbosity.
    /// </summary>
    Quiet = 0,

    /// <summary>
    /// Minimal verbosity.
    /// </summary>
    Minimal = 1,

    /// <summary>
    /// Normal verbosity.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Detailed verbosity.
    /// </summary>
    Detailed = 3,

    /// <summary>
    /// Diagnostic verbosity.
    /// </summary>
    Diagnostic = 4
}
