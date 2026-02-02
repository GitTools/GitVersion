using GitVersion.Git;

namespace GitVersion;

public class GitVersionOptions
{
    public string WorkingDirectory { get; set; } = SysEnv.CurrentDirectory;
    public AssemblySettingsInfo AssemblySettingsInfo { get; } = new();
    public AuthenticationInfo AuthenticationInfo { get; } = new();

    public ConfigurationInfo ConfigurationInfo { get; } = new();
    public RepositoryInfo RepositoryInfo { get; } = new();
    public WixInfo WixInfo { get; } = new();
    public Settings Settings { get; } = new();

    public bool Diag { get; init; }
    public bool IsVersion { get; init; }
    public bool IsHelp { get; init; }

    public string? LogFilePath { get; init; }
    public string? ShowVariable { get; init; }
    public string? Format { get; init; }
    public string? OutputFile { get; init; }
    public ISet<OutputType> Output { get; init; } = new HashSet<OutputType>();
}
