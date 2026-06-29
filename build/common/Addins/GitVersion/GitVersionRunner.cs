using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Common.Addins.GitVersion;

/// <summary>
/// The GitVersion runner.
/// </summary>
public sealed partial class GitVersionRunner : Tool<GitVersionSettings>
{
    private readonly ICakeLog _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitVersionRunner"/> class.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="environment">The environment.</param>
    /// <param name="processRunner">The process runner.</param>
    /// <param name="tools">The tool locator.</param>
    /// <param name="log">The log.</param>
    public GitVersionRunner(
        IFileSystem fileSystem,
        ICakeEnvironment environment,
        IProcessRunner processRunner,
        IToolLocator tools,
        ICakeLog log) : base(fileSystem, environment, processRunner, tools) => this._log = log;

    /// <summary>
    /// Runs GitVersion and processes the results.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <returns>A task with the GitVersion results.</returns>
    public GitVersion Run(GitVersionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var output = string.Empty;
        Run(settings, GetArguments(settings), new ProcessSettings { RedirectStandardOutput = true }, process =>
        {
            output = string.Join("\n", process.GetStandardOutput());
            if (this._log.Verbosity >= Verbosity.Diagnostic) return;
            var regex = ParseErrorRegex();
            var errors = regex.Matches(output)
                .SelectMany(match => new[] { match.Groups[1].Value, match.Groups[2].Value });
            foreach (var error in errors)
            {
                this._log.Error(error);
            }
        });

        if (!settings.OutputTypes.Contains(GitVersionOutput.Json))
            return new GitVersion();

        var jsonStartIndex = output.IndexOf('{');
        var jsonEndIndex = output.IndexOf('}');
        var json = output.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

        return JsonConvert.DeserializeObject<GitVersion>(json) ?? new GitVersion();
    }

    private ProcessArgumentBuilder GetArguments(GitVersionSettings settings)
    {
        var builder = new ProcessArgumentBuilder();

        AppendOutputArguments(builder, settings);

        if (!string.IsNullOrWhiteSpace(settings.ShowVariable))
        {
            builder.Append("-showvariable");
            builder.Append(settings.ShowVariable);
        }

        AppendAuthenticationArguments(builder, settings);
        AppendAssemblyInfoArguments(builder, settings);
        AppendRepositoryArguments(builder, settings);

        if (settings.LogFilePath != null)
        {
            builder.Append("-l");
            builder.AppendQuoted(settings.LogFilePath.FullPath);
        }

        if (settings.NoFetch)
        {
            builder.Append("-nofetch");
        }

        AppendVerbosityArguments(builder, settings);

        return builder;
    }

    private static void AppendOutputArguments(ProcessArgumentBuilder builder, GitVersionSettings settings)
    {
        if (settings.OutputTypes.Contains(GitVersionOutput.Json))
        {
            builder.Append("-output");
            builder.Append("json");
        }

        if (settings.OutputTypes.Contains(GitVersionOutput.BuildServer))
        {
            builder.Append("-output");
            builder.Append("buildserver");
        }
    }

    private static void AppendAuthenticationArguments(ProcessArgumentBuilder builder, GitVersionSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.UserName))
        {
            return;
        }

        builder.Append("-u");
        builder.AppendQuoted(settings.UserName);

        builder.Append("-p");
        builder.AppendQuotedSecret(settings.Password);
    }

    private static void AppendAssemblyInfoArguments(ProcessArgumentBuilder builder, GitVersionSettings settings)
    {
        if (!settings.UpdateAssemblyInfo)
        {
            return;
        }

        builder.Append("-updateassemblyinfo");

        if (settings.UpdateAssemblyInfoFilePath != null)
        {
            builder.AppendQuoted(settings.UpdateAssemblyInfoFilePath.FullPath);
        }
    }

    private void AppendRepositoryArguments(ProcessArgumentBuilder builder, GitVersionSettings settings)
    {
        if (settings.RepositoryPath != null)
        {
            builder.Append("-targetpath");
            builder.AppendQuoted(settings.RepositoryPath.FullPath);
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.Url))
        {
            return;
        }

        builder.Append("-url");
        builder.AppendQuoted(settings.Url);

        if (!string.IsNullOrWhiteSpace(settings.Branch))
        {
            builder.Append("-b");
            builder.Append(settings.Branch);
        }
        else
        {
            this._log.Warning(
                "If you leave the branch name for GitVersion unset, it will fallback to the default branch for the repository.");
        }

        if (!string.IsNullOrWhiteSpace(settings.Commit))
        {
            builder.Append("-c");
            builder.AppendQuoted(settings.Commit);
        }

        if (settings.DynamicRepositoryPath != null)
        {
            builder.Append("-dynamicRepoLocation");
            builder.AppendQuoted(settings.DynamicRepositoryPath.FullPath);
        }
    }

    private void AppendVerbosityArguments(ProcessArgumentBuilder builder, GitVersionSettings settings)
    {
        var verbosity = settings.Verbosity ?? this._log.Verbosity;

        if (verbosity != Verbosity.Normal)
        {
            builder.Append("-verbosity");
            builder.Append(verbosity.ToString());
        }
    }

    /// <summary>
    /// Gets the name of the tool.
    /// </summary>
    /// <returns>The name of the tool.</returns>
    protected override string GetToolName() => "GitVersion";

    /// <summary>
    /// Gets the possible names of the tool executable.
    /// </summary>
    /// <returns>The tool executable name.</returns>
    protected override IEnumerable<string> GetToolExecutableNames() =>
    [
        "GitVersion.exe",
        "dotnet-gitversion",
        "dotnet-gitversion.exe",
        "gitversion"
    ];

    [GeneratedRegex(@"( *ERROR:? [^\n]*)\n([^\n]*)")]
    private static partial Regex ParseErrorRegex();
}
