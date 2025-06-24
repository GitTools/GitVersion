using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion;

internal class Arguments
{
    public AuthenticationInfo Authentication = new();

    public string? ConfigurationFile;
    public IReadOnlyDictionary<object, object?> OverrideConfiguration;
    public bool ShowConfiguration;

    public string? TargetPath;

    public string? TargetUrl;
    public string? TargetBranch;
    public string? CommitId;
    public string? ClonePath;

    public bool Diag;
    public bool IsVersion;
    public bool IsHelp;

    public bool NoFetch;
    public bool NoCache;
    public bool NoNormalize;
    public bool AllowShallow;

    public string? LogFilePath;
    public string? ShowVariable;
    public string? Format;
    public string? OutputFile;
    public ISet<OutputType> Output = new HashSet<OutputType>();
    public Verbosity Verbosity = Verbosity.Normal;

    public bool UpdateWixVersionFile;
    public bool UpdateProjectFiles;
    public bool UpdateAssemblyInfo;
    public bool EnsureAssemblyInfo;
    public ISet<string> UpdateAssemblyInfoFileName = new HashSet<string>();

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
                Username = this.Authentication.Username,
                Password = this.Authentication.Password,
                Token = this.Authentication.Token
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

        var workingDirectory = this.TargetPath?.TrimEnd('/', '\\');
        if (workingDirectory != null)
        {
            gitVersionOptions.WorkingDirectory = workingDirectory;
        }

        return gitVersionOptions;
    }
}
