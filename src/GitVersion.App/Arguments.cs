using GitVersion.Git;

namespace GitVersion;

internal class Arguments
{
    public AuthenticationInfo Authentication = new();

    public string? ConfigurationFile;
    public IReadOnlyDictionary<object, object?> OverrideConfiguration = new Dictionary<object, object?>();
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
                UpdateProjectFiles = this.UpdateProjectFiles,
                UpdateAssemblyInfo = this.UpdateAssemblyInfo,
                EnsureAssemblyInfo = this.EnsureAssemblyInfo,
                Files = this.UpdateAssemblyInfoFileName
            },

            AuthenticationInfo =
            {
                Username = this.Authentication.Username,
                Password = this.Authentication.Password,
                Token = this.Authentication.Token
            },

            ConfigurationInfo =
            {
                ConfigurationFile = this.ConfigurationFile,
                OverrideConfiguration = this.OverrideConfiguration,
                ShowConfiguration = this.ShowConfiguration
            },

            RepositoryInfo =
            {
                TargetUrl = this.TargetUrl,
                TargetBranch = this.TargetBranch,
                CommitId = this.CommitId,
                ClonePath = this.ClonePath
            },

            Settings =
            {
                NoFetch = this.NoFetch,
                NoCache = this.NoCache,
                NoNormalize = this.NoNormalize,
                AllowShallow = this.AllowShallow
            },

            WixInfo =
            {
                UpdateWixVersionFile = this.UpdateWixVersionFile
            },

            Diag = this.Diag,
            IsVersion = this.IsVersion,
            IsHelp = this.IsHelp,

            LogFilePath = this.LogFilePath,
            ShowVariable = this.ShowVariable,
            Format = this.Format,
            Output = this.Output,
            OutputFile = this.OutputFile
        };

        var workingDirectory = this.TargetPath?.TrimEnd('/', '\\');
        if (workingDirectory != null)
        {
            var absolutePath = Path.GetFullPath(workingDirectory);
            gitVersionOptions.WorkingDirectory = absolutePath;
        }

        return gitVersionOptions;
    }
}
