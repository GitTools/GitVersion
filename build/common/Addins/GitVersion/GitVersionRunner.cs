using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Common.Addins.GitVersion;

/// <summary>
/// The GitVersion runner.
/// </summary>
public sealed class GitVersionRunner : Tool<GitVersionSettings>
{
    private readonly ICakeLog log;

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
        ICakeLog log) : base(fileSystem, environment, processRunner, tools) => this.log = log;

    /// <summary>
    /// Runs GitVersion and processes the results.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <returns>A task with the GitVersion results.</returns>
    public GitVersion Run(GitVersionSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var output = string.Empty;
        Run(settings, GetArguments(settings), new ProcessSettings { RedirectStandardOutput = true }, process =>
        {
            output = string.Join("\n", process.GetStandardOutput());
            if (log.Verbosity < Verbosity.Diagnostic)
            {
                var errors = Regex.Matches(output, @"( *ERROR:? [^\n]*)\n([^\n]*)").Cast<Match>()
                    .SelectMany(match => new[] { match.Groups[1].Value, match.Groups[2].Value });
                foreach (var error in errors)
                {
                    log.Error(error);
                }
            }
        });

        if (!settings.OutputTypes.Contains(GitVersionOutput.Json))
            return new GitVersion();

        var jsonStartIndex = output.IndexOf("{", StringComparison.Ordinal);
        var jsonEndIndex = output.IndexOf("}", StringComparison.Ordinal);
        var json = output.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);

        return JsonConvert.DeserializeObject<GitVersion>(json) ?? new GitVersion();
    }

    private ProcessArgumentBuilder GetArguments(GitVersionSettings settings)
    {
        var builder = new ProcessArgumentBuilder();

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

        if (!string.IsNullOrWhiteSpace(settings.ShowVariable))
        {
            builder.Append("-showvariable");
            builder.Append(settings.ShowVariable);
        }

        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            builder.Append("-u");
            builder.AppendQuoted(settings.UserName);

            builder.Append("-p");
            builder.AppendQuotedSecret(settings.Password);
        }

        if (settings.UpdateAssemblyInfo)
        {
            builder.Append("-updateassemblyinfo");

            if (settings.UpdateAssemblyInfoFilePath != null)
            {
                builder.AppendQuoted(settings.UpdateAssemblyInfoFilePath.FullPath);
            }
        }

        if (settings.RepositoryPath != null)
        {
            builder.Append("-targetpath");
            builder.AppendQuoted(settings.RepositoryPath.FullPath);
        }
        else if (!string.IsNullOrWhiteSpace(settings.Url))
        {
            builder.Append("-url");
            builder.AppendQuoted(settings.Url);

            if (!string.IsNullOrWhiteSpace(settings.Branch))
            {
                builder.Append("-b");
                builder.Append(settings.Branch);
            }
            else
            {
                log.Warning("If you leave the branch name for GitVersion unset, it will fallback to the default branch for the repository.");
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

        if (settings.LogFilePath != null)
        {
            builder.Append("-l");
            builder.AppendQuoted(settings.LogFilePath.FullPath);
        }

        if (settings.NoFetch)
        {
            builder.Append("-nofetch");
        }

        var verbosity = settings.Verbosity ?? log.Verbosity;

        if (verbosity != Verbosity.Normal)
        {
            builder.Append("-verbosity");
            builder.Append(verbosity.ToString());
        }
        return builder;
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
    protected override IEnumerable<string> GetToolExecutableNames() => new[] { "GitVersion.exe", "dotnet-gitversion", "dotnet-gitversion.exe", "gitversion" };
}
