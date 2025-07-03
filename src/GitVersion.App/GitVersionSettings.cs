using System.ComponentModel;
using Spectre.Console.Cli;

namespace GitVersion;

/// <summary>
/// Settings class for Spectre.Console.Cli with POSIX compliant options
/// </summary>
internal class GitVersionSettings : CommandSettings
{
    [CommandArgument(0, "[path]")]
    [Description("Path to the Git repository (defaults to current directory)")]
    public string? TargetPath { get; set; }

    [CommandOption("--config")]
    [Description("Path to GitVersion configuration file")]
    public string? ConfigurationFile { get; set; }

    [CommandOption("--show-config")]
    [Description("Display the effective GitVersion configuration and exit")]
    public bool ShowConfiguration { get; set; }

    [CommandOption("--override-config")]
    [Description("Override GitVersion configuration values")]
    public Dictionary<string, string>? OverrideConfiguration { get; set; }

    [CommandOption("-o|--output")]
    [Description("Output format (json, file, buildserver, console)")]
    public string[]? Output { get; set; }

    [CommandOption("--output-file")]
    [Description("Output file when using file output")]
    public string? OutputFile { get; set; }

    [CommandOption("-f|--format")]
    [Description("Format string for version output")]
    public string? Format { get; set; }

    [CommandOption("--show-variable")]
    [Description("Show a specific GitVersion variable")]
    public string? ShowVariable { get; set; }

    [CommandOption("--url")]
    [Description("Remote repository URL")]
    public string? Url { get; set; }

    [CommandOption("-b|--branch")]
    [Description("Target branch name")]
    public string? Branch { get; set; }

    [CommandOption("-c|--commit")]
    [Description("Target commit SHA")]
    public string? Commit { get; set; }

    [CommandOption("--target-path")]
    [Description("Same as positional path argument")]
    public string? TargetPathOption { get; set; }

    [CommandOption("--dynamic-repo-location")]
    [Description("Path to clone remote repository")]
    public string? DynamicRepoLocation { get; set; }

    [CommandOption("-u|--username")]
    [Description("Username for remote repository authentication")]
    public string? Username { get; set; }

    [CommandOption("-p|--password")]
    [Description("Password for remote repository authentication")]
    public string? Password { get; set; }

    [CommandOption("--no-fetch")]
    [Description("Disable Git fetch")]
    public bool NoFetch { get; set; }

    [CommandOption("--no-cache")]
    [Description("Disable GitVersion result caching")]
    public bool NoCache { get; set; }

    [CommandOption("--no-normalize")]
    [Description("Disable branch name normalization")]
    public bool NoNormalize { get; set; }

    [CommandOption("--allow-shallow")]
    [Description("Allow operation on shallow Git repositories")]
    public bool AllowShallow { get; set; }

    [CommandOption("--diag")]
    [Description("Enable diagnostic output")]
    public bool Diag { get; set; }

    [CommandOption("--update-assembly-info")]
    [Description("Update AssemblyInfo files")]
    public bool UpdateAssemblyInfo { get; set; }

    [CommandOption("--ensure-assembly-info")]
    [Description("Ensure AssemblyInfo files exist")]
    public bool EnsureAssemblyInfo { get; set; }

    [CommandOption("--update-assembly-info-filename")]
    [Description("Specific AssemblyInfo files to update")]
    public string[]? UpdateAssemblyInfoFileName { get; set; }

    [CommandOption("--update-project-files")]
    [Description("Update MSBuild project files")]
    public bool UpdateProjectFiles { get; set; }

    [CommandOption("--update-wix-version-file")]
    [Description("Update WiX version file")]
    public bool UpdateWixVersionFile { get; set; }

    [CommandOption("-l|--log-file")]
    [Description("Path to log file")]
    public string? LogFilePath { get; set; }

    [CommandOption("-v|--verbosity")]
    [Description("Logging verbosity (quiet, minimal, normal, verbose, diagnostic)")]
    public string? Verbosity { get; set; }
}