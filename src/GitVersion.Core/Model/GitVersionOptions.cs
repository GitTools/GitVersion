using GitVersion.Logging;
using GitVersion.Model;

namespace GitVersion;

public class GitVersionOptions
{
    public string? WorkingDirectory { get; set; }

    public AssemblyInfoData AssemblyInfo { get; } = new AssemblyInfoData();
    public AuthenticationInfo Authentication { get; } = new AuthenticationInfo();
    public ConfigInfo ConfigInfo { get; } = new ConfigInfo();
    public RepositoryInfo RepositoryInfo { get; } = new RepositoryInfo();
    public WixInfo WixInfo { get; } = new WixInfo();
    public Settings Settings { get; } = new Settings();

    public bool Init;
    public bool Diag;
    public bool IsVersion;
    public bool IsHelp;

    public string? LogFilePath;
    public string? ShowVariable;
    public string? OutputFile;
    public ISet<OutputType> Output = new HashSet<OutputType>();
    public Verbosity Verbosity = Verbosity.Normal;
}
