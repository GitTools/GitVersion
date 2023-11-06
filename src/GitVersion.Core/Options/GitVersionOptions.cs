using GitVersion.Logging;

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

    public bool Init;
    public bool Diag;
    public bool IsVersion;
    public bool IsHelp;

    public string? LogFilePath;
    public string? ShowVariable;
    public string? Format;
    public string? OutputFile;
    public ISet<OutputType> Output = new HashSet<OutputType>();
    public Verbosity Verbosity = Verbosity.Normal;
}
