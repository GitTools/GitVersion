using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion;

/// <summary>Top-level options object that aggregates all settings used to configure a GitVersion execution.</summary>
public class GitVersionOptions
{
    /// <summary>Gets or sets the working directory from which GitVersion should operate.</summary>
    public string WorkingDirectory { get; set; } = SysEnv.CurrentDirectory;

    /// <summary>Gets the assembly-update settings.</summary>
    public AssemblySettingsInfo AssemblySettingsInfo { get; } = new();

    /// <summary>Gets the credentials used when authenticating with a remote repository.</summary>
    public AuthenticationInfo AuthenticationInfo { get; } = new();

    /// <summary>Gets the settings that control how the GitVersion configuration file is located and applied.</summary>
    public ConfigurationInfo ConfigurationInfo { get; } = new();

    /// <summary>Gets the repository-targeting settings (URL, branch, commit, clone path).</summary>
    public RepositoryInfo RepositoryInfo { get; } = new();

    /// <summary>Gets the WiX-specific version-file update settings.</summary>
    public WixInfo WixInfo { get; } = new();

    /// <summary>Gets the general runtime behaviour settings (cache, fetch, normalise).</summary>
    public Settings Settings { get; } = new();

    /// <summary>Gets or sets a value indicating whether extended diagnostic output should be emitted.</summary>
    public bool Diag;

    /// <summary>Gets or sets a value indicating whether the GitVersion version number should be printed and execution should stop.</summary>
    public bool IsVersion;

    /// <summary>Gets or sets a value indicating whether help text should be printed and execution should stop.</summary>
    public bool IsHelp;

    /// <summary>Gets or sets the path to a file where log output should be written.</summary>
    public string? LogFilePath;

    /// <summary>Gets or sets the name of a single version variable to output.</summary>
    public string? ShowVariable;

    /// <summary>Gets or sets the output format string used when writing a single variable.</summary>
    public string? Format;

    /// <summary>Gets or sets the path of the file to which version output is written when the <see cref="OutputType.File"/> output type is selected.</summary>
    public string? OutputFile;

    /// <summary>Gets or sets the set of output types to produce (JSON, build-server, file, dotenv).</summary>
    public ISet<OutputType> Output = new HashSet<OutputType>();

    /// <summary>Gets or sets the minimum log verbosity level.</summary>
    public Verbosity Verbosity = Verbosity.Normal;
}
