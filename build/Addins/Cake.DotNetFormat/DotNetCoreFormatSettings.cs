using System.Collections.Generic;
using Cake.Common.Tools.DotNetCore;
using Cake.Core.IO;

namespace Cake.DotNetFormat
{
    public sealed class DotNetCoreFormatSettings : DotNetCoreSettings
    {
        /// <summary>
        /// A path to a solution file, a project file, or a folder containing a solution or project file.
        /// If a path is not specified then the current directory is used.
        /// </summary>
        public Path? Workspace { get; set; }

        /// <summary>
        /// Whether to treat the <see cref="Workspace"/> argument as a simple folder of files.
        /// </summary>
        public bool Folder { get; set; }

        /// <summary>
        /// Run whitespace formatting. Run by default when not applying fixes.
        /// </summary>
        public bool FixWhitespaces { get; set; }

        /// <summary>
        /// Run code style analyzers and apply fixes.
        /// </summary>
        public string? FixStyle { get; set; }

        /// <summary>
        /// Run 3rd party analyzers and apply fixes.
        /// </summary>
        public string? FixAnalyzers { get; set; }

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
        public bool Check { get; set; }

        /// <summary>
        /// Set the verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]
        /// </summary>
        public new DotNetFormatVerbosity Verbosity { get; set; } = DotNetFormatVerbosity.Normal;
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
}
