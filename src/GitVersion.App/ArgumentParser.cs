using System.CommandLine;
using System.CommandLine.Help;
using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.FileSystemGlobbing;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Serilog.Core;
using Serilog.Events;

namespace GitVersion;

internal class ArgumentParser(
    IEnvironment environment,
    IFileSystem fileSystem,
    IConsole console,
    IGlobbingResolver globbingResolver,
    LoggingLevelSwitch loggingLevelSwitch
)
    : IArgumentParser
{
    private readonly IEnvironment environment = environment.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();
    private readonly IConsole console = console.NotNull();
    private readonly IGlobbingResolver globbingResolver = globbingResolver.NotNull();
    private readonly LoggingLevelSwitch loggingLevelSwitch = loggingLevelSwitch.NotNull();

    private const string DefaultOutputFileName = "GitVersion.json";
    private static readonly IEnumerable<string> availableVariables = GitVersionVariables.AvailableVariables;

    private static readonly Dictionary<Verbosity, LogEventLevel> VerbosityMaps = new()
    {
        { Verbosity.Verbose, LogEventLevel.Verbose },
        { Verbosity.Diagnostic, LogEventLevel.Debug },
        { Verbosity.Normal, LogEventLevel.Information },
        { Verbosity.Minimal, LogEventLevel.Warning },
        { Verbosity.Quiet, LogEventLevel.Error }
    };

    // Build the command schema at once — it's stateless and safe to reuse across calls.
    private static readonly Lazy<(RootCommand Root, CommandOptions Options)> commandFactory = new(BuildCommand);

    public Arguments ParseArguments(string commandLineArguments) =>
        ParseArguments(QuotedStringHelpers.SplitUnquoted(commandLineArguments, ' '));

    public Arguments ParseArguments(string[] commandLineArguments)
    {
        var (rootCommand, options) = commandFactory.Value;
        var parseResult = rootCommand.Parse(commandLineArguments);

        ValidateParsedResult(parseResult, options);

        if (IsOptionExplicitlySet<HelpOption>())
        {
            parseResult.Invoke();
            return new Arguments { IsHelp = true };
        }
        if (IsOptionExplicitlySet<VersionOption>())
        {
            parseResult.Invoke();
            return new Arguments { IsVersion = true };
        }

        var arguments = new Arguments();
        var telemetry = new TelemetryCollectionBuilder(nameof(ArgumentParser));
        AddAuthentication(arguments);
        MapParsedValues(arguments, parseResult, options, commandLineArguments, telemetry);
        arguments.Telemetry = telemetry.Build();
        arguments.TelemetryOptOut = TelemetryPolicy.IsOptedOut(this.environment, parseResult.GetValue(options.TelemetryOptOut));
        ValidateConfigurationFile(arguments);

        return arguments;

        bool IsOptionExplicitlySet<T>() where T : Option
        {
            var option = rootCommand.Options.SingleOfType<T>();
            return parseResult.GetResult(option) is { Implicit: false };
        }
    }

    private static void ValidateParsedResult(ParseResult parseResult, CommandOptions options)
    {
        if (parseResult.Errors.Count > 0)
        {
            var message = parseResult.Errors[0].Message;
            var token = message.Contains("Unrecognized command or argument")
                ? ExtractUnrecognizedToken(message)
                : message;
            throw new WarningException($"Could not parse command line parameter '{token}'.");
        }

        if (parseResult.UnmatchedTokens.Count > 0)
        {
            throw new WarningException($"Could not parse command line parameter '{parseResult.UnmatchedTokens[0]}'.");
        }

        var positionalCheck = parseResult.GetValue(options.Path);
        if (positionalCheck?.StartsWith('-') == true)
        {
            throw new WarningException($"Could not parse command line parameter '{positionalCheck}'.");
        }
    }

    private static string ExtractUnrecognizedToken(string message)
    {
        // System.CommandLine error format: "Unrecognized command or argument 'xxx'."
        var start = message.IndexOf('\'');
        var end = message.LastIndexOf('\'');
        return start >= 0 && end > start ? message[(start + 1)..end] : message;
    }

    private void MapParsedValues(
        Arguments arguments,
        ParseResult parseResult,
        CommandOptions options,
        string[] commandLineArguments,
        TelemetryCollectionBuilder telemetry)
    {
        if (commandLineArguments.Length > 0 && !commandLineArguments[0].StartsWith('-'))
        {
            telemetry.AddValue(TelemetryArgumentNames.Path, commandLineArguments[0], TelemetryValueKind.Path);
        }

        if (parseResult.GetResult(options.LogFile) is { Implicit: false })
        {
            arguments.LogFilePath = parseResult.GetValue(options.LogFile) ?? arguments.LogFilePath;
            telemetry.AddValue(TelemetryArgumentNames.LogFile, arguments.LogFilePath, TelemetryValueKind.Path);
        }

        arguments.Diag = parseResult.GetValue(options.Diagnose);
        if (arguments.Diag)
        {
            telemetry.AddFlag(TelemetryArgumentNames.Diagnose);
        }

        arguments.ShowConfiguration = parseResult.GetValue(options.ShowConfig);
        if (arguments.ShowConfiguration)
        {
            telemetry.AddFlag(TelemetryArgumentNames.ShowConfig);
        }

        arguments.NoFetch = parseResult.GetValue(options.NoFetch);
        if (arguments.NoFetch)
        {
            telemetry.AddFlag(TelemetryArgumentNames.NoFetch);
        }

        arguments.NoCache = parseResult.GetValue(options.NoCache);
        if (arguments.NoCache)
        {
            telemetry.AddFlag(TelemetryArgumentNames.NoCache);
        }

        arguments.NoNormalize = parseResult.GetValue(options.NoNormalize);
        if (arguments.NoNormalize)
        {
            telemetry.AddFlag(TelemetryArgumentNames.NoNormalize);
        }

        arguments.AllowShallow = parseResult.GetValue(options.AllowShallow);
        if (arguments.AllowShallow)
        {
            telemetry.AddFlag(TelemetryArgumentNames.AllowShallow);
        }

        arguments.UpdateWixVersionFile = parseResult.GetValue(options.UpdateWixVersionFile);
        if (arguments.UpdateWixVersionFile)
        {
            telemetry.AddFlag(TelemetryArgumentNames.UpdateWixVersionFile);
        }

        if (parseResult.GetResult(options.Output) is { Implicit: false } && parseResult.GetValue(options.Output) is { } outputs)
        {
            foreach (var output in outputs)
            {
                arguments.Output.Add(output);
            }

            telemetry.AddValues(TelemetryArgumentNames.Output, outputs.Select(output => output.ToString().ToLowerInvariant()));
        }

        if (parseResult.GetResult(options.OutputFile) is { Implicit: false } && parseResult.GetValue(options.OutputFile) is { } outputFile)
        {
            arguments.OutputFile = outputFile;
            telemetry.AddValue(TelemetryArgumentNames.OutputFile, outputFile, TelemetryValueKind.Path);
        }

        if (parseResult.GetResult(options.ShowVariable) is { Implicit: false } && parseResult.GetValue(options.ShowVariable) is { } showVariable)
        {
            ParseShowVariable(arguments, showVariable);
            telemetry.AddValue(TelemetryArgumentNames.ShowVariable, showVariable);
        }

        if (parseResult.GetResult(options.Format) is { Implicit: false } && parseResult.GetValue(options.Format) is { } format)
        {
            ParseFormat(arguments, format);
            telemetry.AddValue(TelemetryArgumentNames.Format, format);
        }

        if (parseResult.GetResult(options.Config) is { Implicit: false } && parseResult.GetValue(options.Config) is { } config)
        {
            arguments.ConfigurationFile = config;
            telemetry.AddValue(TelemetryArgumentNames.Config, config, TelemetryValueKind.Path);
        }

        if (parseResult.GetResult(options.OverrideConfig) is { Implicit: false }
            && parseResult.GetValue(options.OverrideConfig) is { Length: > 0 } overrideConfigs)
        {
            ParseOverrideConfig(arguments, overrideConfigs);
            telemetry.AddValues(TelemetryArgumentNames.OverrideConfig, overrideConfigs);
        }

        if (parseResult.GetResult(options.TargetPath) is { Implicit: false } && parseResult.GetValue(options.TargetPath) is { } targetPath)
        {
            arguments.TargetPath = targetPath;
            if (string.IsNullOrWhiteSpace(targetPath) || !this.fileSystem.Directory.Exists(targetPath))
            {
                this.console.WriteLine($"The working directory '{targetPath}' does not exist.");
            }

            telemetry.AddValue(TelemetryArgumentNames.TargetPath, targetPath, TelemetryValueKind.Path);
        }

        if (parseResult.GetResult(options.VerbosityOption) is { Implicit: false }
            && parseResult.GetValue(options.VerbosityOption) is { } verbosity)
        {
            this.loggingLevelSwitch.MinimumLevel = VerbosityMaps[ParseVerbosity(verbosity)];
            telemetry.AddValue(TelemetryArgumentNames.Verbosity, verbosity.ToLowerInvariant());
        }

        if (parseResult.GetResult(options.UpdateAssemblyInfo) is { Implicit: false })
        {
            var values = parseResult.GetValue(options.UpdateAssemblyInfo);
            if (values is [var single] && (single.Equals("false", StringComparison.OrdinalIgnoreCase) || single.Equals("0", StringComparison.Ordinal)))
            {
                arguments.UpdateAssemblyInfo = false;
            }
            else
            {
                arguments.UpdateAssemblyInfo = true;
                if (values != null)
                {
                    foreach (var file in values.Where(f => !f.IsTrue()))
                    {
                        arguments.UpdateAssemblyInfoFileName.Add(file);
                    }
                }
            }

            if (values is { Length: > 0 })
            {
                telemetry.AddValues(TelemetryArgumentNames.UpdateAssemblyInfo, values, TelemetryValueKind.PathOrBoolean);
            }
            else
            {
                telemetry.AddFlag(TelemetryArgumentNames.UpdateAssemblyInfo);
            }

            if (arguments.UpdateProjectFiles)
            {
                throw new WarningException("Cannot specify both --update-project-files and --update-assembly-info in the same run. Please rerun GitVersion with only one parameter");
            }
        }

        if (parseResult.GetResult(options.UpdateProjectFiles) is { Implicit: false })
        {
            arguments.UpdateProjectFiles = true;
            var projectFiles = parseResult.GetValue(options.UpdateProjectFiles);
            if (projectFiles != null)
            {
                foreach (var file in projectFiles.Where(f => !f.IsTrue()))
                {
                    arguments.UpdateAssemblyInfoFileName.Add(file);
                }
            }

            if (projectFiles is { Length: > 0 })
            {
                telemetry.AddValues(TelemetryArgumentNames.UpdateProjectFiles, projectFiles, TelemetryValueKind.PathOrBoolean);
            }
            else
            {
                telemetry.AddFlag(TelemetryArgumentNames.UpdateProjectFiles);
            }

            if (arguments.UpdateAssemblyInfo)
            {
                throw new WarningException("Cannot specify both --update-assembly-info and --update-project-files in the same run. Please rerun GitVersion with only one parameter");
            }

            if (arguments.EnsureAssemblyInfo)
            {
                throw new WarningException("Cannot specify --ensure-assembly-info with --update-project-files: please ensure your project file exists before attempting to update it");
            }
        }

        if (parseResult.GetValue(options.EnsureAssemblyInfo))
        {
            arguments.EnsureAssemblyInfo = true;
            telemetry.AddFlag(TelemetryArgumentNames.EnsureAssemblyInfo);

            if (arguments.UpdateProjectFiles)
            {
                throw new WarningException("Cannot specify --ensure-assembly-info with --update-project-files: please ensure your project file exists before attempting to update it");
            }
        }

        if (arguments.UpdateAssemblyInfoFileName.Count > 1 && arguments.EnsureAssemblyInfo)
        {
            throw new WarningException("Can't specify multiple assembly info files when using --ensure-assembly-info, either use a single assembly info file or do not specify --ensure-assembly-info and create assembly info files manually");
        }

        if (parseResult.GetResult(options.Url) is { Implicit: false } && parseResult.GetValue(options.Url) is { } url)
        {
            arguments.TargetUrl = url;
            telemetry.AddValue(TelemetryArgumentNames.Url, url, TelemetryValueKind.Sensitive);
        }

        if (parseResult.GetResult(options.Branch) is { Implicit: false } && parseResult.GetValue(options.Branch) is { } branch)
        {
            arguments.TargetBranch = branch;
            telemetry.AddValue(TelemetryArgumentNames.Branch, branch);
        }

        if (parseResult.GetResult(options.Username) is { Implicit: false } && parseResult.GetValue(options.Username) is { } username)
        {
            arguments.Authentication.Username = username;
            telemetry.AddValue(TelemetryArgumentNames.Username, username, TelemetryValueKind.Sensitive);
        }

        if (parseResult.GetResult(options.Password) is { Implicit: false } && parseResult.GetValue(options.Password) is { } password)
        {
            arguments.Authentication.Password = password;
            telemetry.AddValue(TelemetryArgumentNames.Password, password, TelemetryValueKind.Sensitive);
        }

        if (parseResult.GetResult(options.Commit) is { Implicit: false } && parseResult.GetValue(options.Commit) is { } commit)
        {
            arguments.CommitId = commit;
            telemetry.AddValue(TelemetryArgumentNames.Commit, commit);
        }

        if (parseResult.GetResult(options.DynamicRepoLocation) is { Implicit: false }
            && parseResult.GetValue(options.DynamicRepoLocation) is { } dynRepo)
        {
            arguments.ClonePath = dynRepo;
            telemetry.AddValue(TelemetryArgumentNames.DynamicRepoLocation, dynRepo, TelemetryValueKind.Path);
        }

        if (parseResult.GetValue(options.TelemetryOptOut))
        {
            telemetry.AddFlag(TelemetryArgumentNames.TelemetryOptOut);
        }

        if (arguments.Output.Count == 0)
        {
            arguments.Output.Add(OutputType.Json);
        }

        if (arguments.Output.Contains(OutputType.File) && arguments.OutputFile == null)
        {
            arguments.OutputFile = DefaultOutputFileName;
        }

        arguments.TargetPath ??= parseResult.GetValue(options.Path) ?? SysEnv.CurrentDirectory;
        arguments.TargetPath = arguments.TargetPath.TrimEnd('/', '\\');

        if (!arguments.EnsureAssemblyInfo)
        {
            arguments.UpdateAssemblyInfoFileName = ResolveFiles(arguments.TargetPath, arguments.UpdateAssemblyInfoFileName).ToHashSet();
        }
    }

    private static (RootCommand Root, CommandOptions Options) BuildCommand()
    {
        var path = new Argument<string?>("path")
        {
            Description = "The directory containing .git. If not defined current directory is used. (Must be first argument)",
            Arity = ArgumentArity.ZeroOrOne
        };
        var diagnose = new Option<bool>("--diagnose", "-d")
        {
            Description = """
                          Runs GitVersion with additional diagnostic information.
                          Also needs the '--log-file' argument to specify a logfile or stdout (requires git.exe to be installed)
                          """
        };
        var logFile = new Option<string?>("--log-file", "-l")
        {
            Description = "Path to logfile; specify 'console' to emit to stdout"
        };
        var output = new Option<OutputType[]>("--output", "-o")
        {
            Description = "Determines the output to the console. Can be either 'json', 'file', 'buildserver' or 'dotenv', will default to 'json'",
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore
        };
        var outputFile = new Option<string?>("--output-file")
        {
            Description = "Path to output file. It is used in combination with --output 'file'"
        };
        var showVariable = new Option<string?>("--show-variable", "-v")
        {
            Description = """
                          Used in conjunction with --output json, will output just a particular variable.
                          E.g. --output json --show-variable SemVer - will output `1.2.3+beta.4`
                          """
        };
        var format = new Option<string?>("--format", "-f")
        {
            Description = """
                          Used in conjunction with --output json, will output a format containing version variables.
                          Supports C# format strings - see [Format Strings](/docs/reference/custom-formatting) for details.
                          E.g. --output json --format {SemVer} - will output `1.2.3+beta.4`
                               --output json --format {Major}.{Minor} - will output `1.2`
                          """
        };
        var config = new Option<string?>("--config", "-c")
        {
            Description = "Path to config file (defaults to GitVersion.yml, GitVersion.yaml, .GitVersion.yml or .GitVersion.yaml)"
        };
        var showConfig = new Option<bool>("--show-config")
        {
            Description = "Outputs the effective GitVersion config (defaults + custom from GitVersion.yml) in yaml format"
        };
        var overrideConfig = new Option<string[]>("--override-config")
        {
            Description = "Overrides GitVersion config values inline (key=value pairs e.g. --override-config tag-prefix=Foo)",
            AllowMultipleArgumentsPerToken = false,
            Arity = ArgumentArity.ZeroOrMore
        };
        var targetPath = new Option<string?>("--target-path")
        {
            Description = "Same as 'path', but not positional"
        };
        var noFetch = new Option<bool>("--no-fetch")
        {
            Description = "Disables 'git fetch' during version calculation. Might cause GitVersion to not calculate your version as expected"
        };
        var noCache = new Option<bool>("--no-cache")
        {
            Description = "Bypasses the cache, result will not be written to the cache"
        };
        var noNormalize = new Option<bool>("--no-normalize")
        {
            Description = "Disables normalize step on a build server"
        };
        var allowShallow = new Option<bool>("--allow-shallow")
        {
            Description = """
                          Allows GitVersion to run on a shallow clone.
                          This is not recommended, but can be used if you are sure that the shallow clone contains all the information needed to calculate the version.
                          """
        };
        var verbosity = new Option<string?>("--verbosity")
        {
            Description = "Specifies the amount of information to be displayed (Quiet, Minimal, Normal, Verbose, Diagnostic). Default is Normal"
        };
        var updateAssemblyInfo = new Option<string[]?>("--update-assembly-info")
        {
            Description = "Will recursively search for all 'AssemblyInfo.cs' files in the git repo and update them",
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore
        };
        var updateProjectFiles = new Option<string[]?>("--update-project-files")
        {
            Description = """
                          Will recursively search for all project files (.csproj/.vbproj/.fsproj/.sqlproj) in the git repo and update them (only compatible with Sdk projects)
                          """,
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore
        };
        var ensureAssemblyInfo = new Option<bool>("--ensure-assembly-info")
        {
            Description = """
                          If the assembly info file specified with --update-assembly-info is not found, it will be created with AssemblyFileVersion, AssemblyVersion and AssemblyInformationalVersion.
                          Supports C#, F#, VB
                          """
        };
        var updateWixVersionFile = new Option<bool>("--update-wix-version-file")
        {
            Description = "All the GitVersion variables are written to 'GitVersion_WixVersion.wxi'"
        };
        var url = new Option<string?>("--url")
        {
            Description = "Url to remote git repository"
        };
        var branch = new Option<string?>("--branch", "-b")
        {
            Description = "Name of the branch to use on the remote repository, must be used in combination with --url"
        };
        var username = new Option<string?>("--username", "-u")
        {
            Description = "Username in case authentication is required"
        };
        var password = new Option<string?>("--password", "-p")
        {
            Description = "Password in case authentication is required"
        };
        var commit = new Option<string?>("--commit")
        {
            Description = "The commit id to check. If not specified, the latest available commit on the specified branch will be used"
        };
        var dynamicRepoLocation = new Option<string?>("--dynamic-repo-location")
        {
            Description = "By default dynamic repositories will be cloned to %tmp%. Use this option to override"
        };
        var telemetryOptOut = new Option<bool>("--telemetry-opt-out")
        {
            Description = "Disables telemetry for this invocation"
        };

        var rootCommand = new RootCommand("Use convention to derive a SemVer product version from a GitFlow or GitHub based repository.")
        {
            path,
            diagnose,
            logFile,
            output,
            outputFile,
            showVariable,
            format,
            config,
            showConfig,
            overrideConfig,
            targetPath,
            noFetch,
            noCache,
            noNormalize,
            allowShallow,
            verbosity,
            updateAssemblyInfo,
            updateProjectFiles,
            ensureAssemblyInfo,
            updateWixVersionFile,
            url,
            branch,
            username,
            password,
            commit,
            dynamicRepoLocation,
            telemetryOptOut
        };

        // Configure the built-in help system to wrap at 260 characters to avoid too small help messages
        var helpOption = rootCommand.Options.SingleOfType<HelpOption>();
        if (helpOption.Action is HelpAction helpAction)
        {
            helpAction.MaxWidth = 260;
        }

        return (rootCommand, new CommandOptions(
            Path: path, Diagnose: diagnose, LogFile: logFile,
            Output: output, OutputFile: outputFile, ShowVariable: showVariable, Format: format,
            Config: config, ShowConfig: showConfig, OverrideConfig: overrideConfig, TargetPath: targetPath,
            NoFetch: noFetch, NoCache: noCache, NoNormalize: noNormalize, AllowShallow: allowShallow,
            VerbosityOption: verbosity, UpdateAssemblyInfo: updateAssemblyInfo, UpdateProjectFiles: updateProjectFiles,
            EnsureAssemblyInfo: ensureAssemblyInfo, UpdateWixVersionFile: updateWixVersionFile,
            Url: url, Branch: branch, Username: username, Password: password,
            Commit: commit, DynamicRepoLocation: dynamicRepoLocation, TelemetryOptOut: telemetryOptOut
        ));
    }

    private void AddAuthentication(Arguments arguments)
    {
        var username = this.environment.GetEnvironmentVariable("GITVERSION_REMOTE_USERNAME");
        var password = this.environment.GetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD");
        if (!username.IsNullOrWhiteSpace())
        {
            arguments.Authentication.Username = username;
        }

        if (!password.IsNullOrWhiteSpace())
        {
            arguments.Authentication.Password = password;
        }
    }

    private IEnumerable<string> ResolveFiles(string workingDirectory, ISet<string>? assemblyInfoFiles) =>
        assemblyInfoFiles?.SelectMany(file => this.globbingResolver.Resolve(workingDirectory, file))
        ?? [];

    private void ValidateConfigurationFile(Arguments arguments)
    {
        if (arguments.ConfigurationFile.IsNullOrWhiteSpace())
        {
            return;
        }

        if (FileSystemHelper.Path.IsPathRooted(arguments.ConfigurationFile))
        {
            if (!this.fileSystem.File.Exists(arguments.ConfigurationFile))
            {
                throw new WarningException($"Could not find config file at '{arguments.ConfigurationFile}'");
            }

            arguments.ConfigurationFile = FileSystemHelper.Path.GetFullPath(arguments.ConfigurationFile);
        }
        else
        {
            var configFilePath = FileSystemHelper.Path.GetFullPath(
                FileSystemHelper.Path.Combine(arguments.TargetPath, arguments.ConfigurationFile));
            if (!this.fileSystem.File.Exists(configFilePath))
            {
                throw new WarningException($"Could not find config file at '{configFilePath}'");
            }

            arguments.ConfigurationFile = configFilePath;
        }
    }

    private static void ParseShowVariable(Arguments arguments, string value)
    {
        var versionVariable = value.IsNullOrWhiteSpace() ? null : availableVariables.SingleOrDefault(av => av.Equals(value.Replace("'", ""), StringComparison.CurrentCultureIgnoreCase));

        if (versionVariable == null)
        {
            var available = string.Join(", ", availableVariables.Select(x => $"'{x}'"));
            throw new WarningException($"--show-variable requires a valid version variable. Available variables are:{FileSystemHelper.Path.NewLine}{available}");
        }

        arguments.ShowVariable = versionVariable;
    }

    private static void ParseFormat(Arguments arguments, string value)
    {
        if (value.IsNullOrWhiteSpace() || !availableVariables.Any(v => value.Contains(v, StringComparison.CurrentCultureIgnoreCase)))
        {
            throw new WarningException("Format requires a valid format string. Available variables are: " +
                                       string.Join(", ", availableVariables));
        }

        arguments.Format = value;
    }

    internal static Verbosity ParseVerbosity(string? value)
    {
        if (!Enum.TryParse(value, true, out Verbosity verbosity))
        {
            throw new WarningException($"Could not parse Verbosity value '{value}'");
        }

        return verbosity;
    }

    private static void ParseOverrideConfig(Arguments arguments, IReadOnlyCollection<string>? values)
    {
        if (values == null || values.Count == 0)
        {
            return;
        }

        var parser = new OverrideConfigurationOptionParser();

        foreach (var keyValueOption in values)
        {
            var keyAndValue = QuotedStringHelpers.SplitUnquoted(keyValueOption, '=');
            if (keyAndValue.Length != 2)
            {
                throw new WarningException($"Could not parse --override-config option: {keyValueOption}. Ensure it is in format 'key=value'.");
            }

            var optionKey = keyAndValue[0].ToLowerInvariant();
            if (!OverrideConfigurationOptionParser.SupportedProperties.Contains(optionKey))
            {
                throw new WarningException($"Could not parse --override-config option: {keyValueOption}. Unsupported key '{optionKey}'.");
            }

            parser.SetValue(optionKey, keyAndValue[1]);
        }

        arguments.OverrideConfiguration = parser.GetOverrideConfiguration();
    }

    private sealed record CommandOptions(
        Argument<string?> Path,
        Option<bool> Diagnose,
        Option<string?> LogFile,
        Option<OutputType[]> Output,
        Option<string?> OutputFile,
        Option<string?> ShowVariable,
        Option<string?> Format,
        Option<string?> Config,
        Option<bool> ShowConfig,
        Option<string[]> OverrideConfig,
        Option<string?> TargetPath,
        Option<bool> NoFetch,
        Option<bool> NoCache,
        Option<bool> NoNormalize,
        Option<bool> AllowShallow,
        Option<string?> VerbosityOption,
        Option<string[]?> UpdateAssemblyInfo,
        Option<string[]?> UpdateProjectFiles,
        Option<bool> EnsureAssemblyInfo,
        Option<bool> UpdateWixVersionFile,
        Option<string?> Url,
        Option<string?> Branch,
        Option<string?> Username,
        Option<string?> Password,
        Option<string?> Commit,
        Option<string?> DynamicRepoLocation,
        Option<bool> TelemetryOptOut
    );
}
