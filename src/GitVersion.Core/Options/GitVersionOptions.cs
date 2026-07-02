using GitVersion.Git;

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

    /// <summary>Gets a value indicating whether extended diagnostic output should be emitted.</summary>
    public bool Diag { get; init; }

    /// <summary>Gets a value indicating whether the GitVersion version number should be printed and execution should stop.</summary>
    public bool IsVersion { get; init; }

    /// <summary>Gets a value indicating whether help text should be printed and execution should stop.</summary>
    public bool IsHelp { get; init; }

    /// <summary>Gets the path to a file where log output should be written.</summary>
    public string? LogFilePath { get; init; }

    /// <summary>Gets the name of a single version variable to output.</summary>
    public string? ShowVariable { get; init; }

    /// <summary>Gets the output format string used when writing a single variable.</summary>
    public string? Format { get; init; }

    /// <summary>Gets the path of the file to which version output is written when the <see cref="OutputType.File"/> output type is selected.</summary>
    public string? OutputFile { get; init; }

    /// <summary>Gets the set of output types to produce (JSON, build-server, file, dotenv).</summary>
    public ISet<OutputType> Output { get; init; } = new HashSet<OutputType>();
}
