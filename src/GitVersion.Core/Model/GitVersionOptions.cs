using GitVersion.Logging;
using GitVersion.Model;

namespace GitVersion;

public class GitVersionOptions
{
    public string? WorkingDirectory { get; set; }

    public AssemblyInfoData AssemblyInfo { get; } = new();
    public AuthenticationInfo Authentication { get; } = new();
    public ConfigInfo ConfigInfo { get; } = new();
    public RepositoryInfo RepositoryInfo { get; } = new();
    public WixInfo WixInfo { get; } = new();
    public Settings Settings { get; } = new();

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
