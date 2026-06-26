using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion;

internal class Arguments
{
    public AuthenticationInfo Authentication { get; set; } = new();
    public string? ConfigurationFile { get; set; }
    public IReadOnlyDictionary<object, object?> OverrideConfiguration { get; set; } = new Dictionary<object, object?>();
    public bool ShowConfiguration { get; set; }
    public string? TargetPath { get; set; }
    public string? TargetUrl { get; set; }
    public string? TargetBranch { get; set; }
    public string? CommitId { get; set; }
    public string? ClonePath { get; set; }
    public bool Diag { get; set; }
    public bool IsVersion { get; set; }
    public bool IsHelp { get; set; }
    public bool NoFetch { get; set; }
    public bool NoCache { get; set; }
    public bool NoNormalize { get; set; }
    public bool AllowShallow { get; set; }
    public string? LogFilePath { get; set; }
    public string? ShowVariable { get; set; }
    public string? Format { get; set; }
    public string? OutputFile { get; set; }
    public ISet<OutputType> Output { get; set; } = new HashSet<OutputType>();
    public Verbosity Verbosity { get; set; } = Verbosity.Normal;
    public bool UpdateWixVersionFile { get; set; }
    public bool UpdateProjectFiles { get; set; }
    public bool UpdateAssemblyInfo { get; set; }
    public bool EnsureAssemblyInfo { get; set; }
    public ISet<string> UpdateAssemblyInfoFileName { get; set; } = new HashSet<string>();

    public GitVersionOptions ToOptions()
    {
        var gitVersionOptions = new GitVersionOptions
        {
            AssemblySettingsInfo =
            {
                UpdateProjectFiles = UpdateProjectFiles,
                UpdateAssemblyInfo = UpdateAssemblyInfo,
                EnsureAssemblyInfo = EnsureAssemblyInfo,
                Files = UpdateAssemblyInfoFileName
            },

            AuthenticationInfo =
            {
                Username = Authentication.Username,
                Password = Authentication.Password,
                Token = Authentication.Token
            },

            ConfigurationInfo =
            {
                ConfigurationFile = ConfigurationFile,
                OverrideConfiguration = OverrideConfiguration,
                ShowConfiguration = ShowConfiguration
            },

            RepositoryInfo =
            {
                TargetUrl = TargetUrl,
                TargetBranch = TargetBranch,
                CommitId = CommitId,
                ClonePath = ClonePath
            },

            Settings =
            {
                NoFetch = NoFetch,
                NoCache = NoCache,
                NoNormalize = NoNormalize,
                AllowShallow = AllowShallow
            },

            WixInfo =
            {
                UpdateWixVersionFile = UpdateWixVersionFile
            },

            Diag = Diag,
            IsVersion = IsVersion,
            IsHelp = IsHelp,

            LogFilePath = LogFilePath,
            ShowVariable = ShowVariable,
            Format = Format,
            Verbosity = Verbosity,
            Output = Output,
            OutputFile = OutputFile
        };

        var workingDirectory = TargetPath?.TrimEnd('/', '\\');
        if (workingDirectory != null)
        {
            gitVersionOptions.WorkingDirectory = workingDirectory;
        }

        return gitVersionOptions;
    }
}
