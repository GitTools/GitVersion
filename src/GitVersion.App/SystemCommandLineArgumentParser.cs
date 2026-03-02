using System.CommandLine;
using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.FileSystemGlobbing;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Serilog.Core;
using Serilog.Events;

namespace GitVersion;

internal class SystemCommandLineArgumentParser(
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

    private const string defaultOutputFileName = "GitVersion.json";
    private static readonly IEnumerable<string> availableVariables = GitVersionVariables.AvailableVariables;

    private static readonly Dictionary<Verbosity, LogEventLevel> VerbosityMaps = new()
    {
        { Verbosity.Verbose, LogEventLevel.Verbose },
        { Verbosity.Diagnostic, LogEventLevel.Debug },
        { Verbosity.Normal, LogEventLevel.Information },
        { Verbosity.Minimal, LogEventLevel.Warning },
        { Verbosity.Quiet, LogEventLevel.Error }
    };

    public Arguments ParseArguments(string commandLineArguments)
    {
        var arguments = QuotedStringHelpers.SplitUnquoted(commandLineArguments, ' ');
        return ParseArguments(arguments);
    }

    public Arguments ParseArguments(string[] commandLineArguments)
    {
        if (commandLineArguments.Length == 0)
        {
            var args = new Arguments
            {
                TargetPath = SysEnv.CurrentDirectory
            };
            args.Output.Add(OutputType.Json);
            AddAuthentication(args);
            return args;
        }

        var (rootCommand, options) = BuildCommand();

        // Let System.CommandLine handle --help output natively
        if (commandLineArguments.Any(a => a is "--help" or "-h" or "-?" or "/?"))
        {
            PrintBuiltInHelp(rootCommand);
            return new Arguments { IsHelp = true };
        }

        // Handle --version before parsing to avoid System.CommandLine interception
        if (commandLineArguments.Any(a => a is "--version"))
        {
            PrintBuiltInVersion();
            return new Arguments { IsVersion = true };
        }

        var parseResult = rootCommand.Parse(commandLineArguments);

        // Check for parse errors
        var errors = parseResult.Errors;
        if (errors.Count > 0)
        {
            var firstError = errors[0];
            var message = firstError.Message;

            // Try to extract the unrecognized token for a friendlier message
            if (message.Contains("Unrecognized command or argument"))
            {
                var token = ExtractUnrecognizedToken(message);
                throw new WarningException($"Could not parse command line parameter '{token}'.");
            }

            throw new WarningException($"Could not parse command line parameter '{message}'.");
        }

        // Check for unmatched tokens that System.CommandLine didn't report as errors
        var unmatchedTokens = parseResult.UnmatchedTokens;
        if (unmatchedTokens.Count > 0)
        {
            throw new WarningException($"Could not parse command line parameter '{unmatchedTokens[0]}'.");
        }

        // Detect unknown options that were incorrectly consumed as positional path argument
        var positionalCheck = parseResult.GetValue(options.Path);
        if (positionalCheck != null && positionalCheck.StartsWith('-'))
        {
            throw new WarningException($"Could not parse command line parameter '{positionalCheck}'.");
        }

        var arguments = new Arguments();
        AddAuthentication(arguments);

        // Map parsed values to Arguments
        MapParsedValues(arguments, parseResult, options);

        // Defaults
        if (arguments.Output.Count == 0)
        {
            arguments.Output.Add(OutputType.Json);
        }

        if (arguments.Output.Contains(OutputType.File) && arguments.OutputFile == null)
        {
            arguments.OutputFile = defaultOutputFileName;
        }

        // Target path
        var positionalPath = parseResult.GetValue(options.Path);
        arguments.TargetPath ??= positionalPath ?? SysEnv.CurrentDirectory;
        arguments.TargetPath = arguments.TargetPath.TrimEnd('/', '\\');

        if (!arguments.EnsureAssemblyInfo)
            arguments.UpdateAssemblyInfoFileName = ResolveFiles(arguments.TargetPath, arguments.UpdateAssemblyInfoFileName).ToHashSet();

        ValidateConfigurationFile(arguments);

        return arguments;
    }

    private void PrintBuiltInHelp(RootCommand rootCommand)
    {
        rootCommand.SetAction((_, _) => Task.FromResult(0));
        rootCommand.Parse(["--help"]).InvokeAsync().GetAwaiter().GetResult();
    }

    private void PrintBuiltInVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
            .FirstOrDefault() is AssemblyInformationalVersionAttribute attr
            ? attr.InformationalVersion
            : assembly.GetName().Version?.ToString();
        if (version != null)
            this.console.WriteLine(version);
    }

    private static string ExtractUnrecognizedToken(string message)
    {
        // System.CommandLine error format: "Unrecognized command or argument 'xxx'."
        var start = message.IndexOf('\'');
        var end = message.LastIndexOf('\'');
        if (start >= 0 && end > start)
        {
            return message[(start + 1)..end];
        }
        return message;
    }

    private void MapParsedValues(Arguments arguments, ParseResult parseResult, CommandOptions options)
    {
        // Log file
        var logFile = parseResult.GetValue(options.LogFile);
        if (logFile != null)
            arguments.LogFilePath = logFile;

        // Diagnose
        if (parseResult.GetValue(options.Diagnose))
            arguments.Diag = true;

        // Output
        var outputs = parseResult.GetValue(options.Output);
        if (outputs != null)
        {
            foreach (var output in outputs)
            {
                arguments.Output.Add(output);
            }
        }

        // Output file
        var outputFile = parseResult.GetValue(options.OutputFile);
        if (outputFile != null)
            arguments.OutputFile = outputFile;

        // Show variable
        var showVariable = parseResult.GetValue(options.ShowVariable);
        if (showVariable != null)
            ParseShowVariable(arguments, showVariable);

        // Format
        var format = parseResult.GetValue(options.Format);
        if (format != null)
            ParseFormat(arguments, format);

        // Config
        var config = parseResult.GetValue(options.Config);
        if (config != null)
            arguments.ConfigurationFile = config;

        // Show config
        if (parseResult.GetValue(options.ShowConfig))
            arguments.ShowConfiguration = true;

        // Override config
        var overrideConfigs = parseResult.GetValue(options.OverrideConfig);
        if (overrideConfigs is { Length: > 0 })
            ParseOverrideConfig(arguments, overrideConfigs);

        // Target path (explicit option)
        var targetPath = parseResult.GetValue(options.TargetPath);
        if (targetPath != null)
        {
            arguments.TargetPath = targetPath;
            if (string.IsNullOrWhiteSpace(targetPath) || !this.fileSystem.Directory.Exists(targetPath))
            {
                this.console.WriteLine($"The working directory '{targetPath}' does not exist.");
            }
        }

        // No-fetch
        if (parseResult.GetValue(options.NoFetch))
            arguments.NoFetch = true;

        // No-cache
        if (parseResult.GetValue(options.NoCache))
            arguments.NoCache = true;

        // No-normalize
        if (parseResult.GetValue(options.NoNormalize))
            arguments.NoNormalize = true;

        // Allow shallow
        if (parseResult.GetValue(options.AllowShallow))
            arguments.AllowShallow = true;

        // Verbosity
        var verbosity = parseResult.GetValue(options.VerbosityOption);
        if (verbosity != null)
        {
            var parsedVerbosity = ParseVerbosity(verbosity);
            this.loggingLevelSwitch.MinimumLevel = VerbosityMaps[parsedVerbosity];
        }

        // Update assembly info
        var updateAssemblyInfoResult = parseResult.GetResult(options.UpdateAssemblyInfo);
        if (updateAssemblyInfoResult is { Implicit: false })
        {
            var updateAssemblyInfo = parseResult.GetValue(options.UpdateAssemblyInfo);

            // Check if the option was explicitly disabled with "false" or "0"
            if (updateAssemblyInfo is { Length: 1 } &&
                (updateAssemblyInfo[0].Equals("false", StringComparison.OrdinalIgnoreCase) ||
                 updateAssemblyInfo[0].Equals("0", StringComparison.Ordinal)))
            {
                arguments.UpdateAssemblyInfo = false;
            }
            else
            {
                arguments.UpdateAssemblyInfo = true;
                if (updateAssemblyInfo != null)
                {
                    foreach (var file in updateAssemblyInfo)
                    {
                        if (!file.Equals("true", StringComparison.OrdinalIgnoreCase) && !file.Equals("1", StringComparison.OrdinalIgnoreCase))
                        {
                            arguments.UpdateAssemblyInfoFileName.Add(file);
                        }
                    }
                }
            }

            if (arguments.UpdateProjectFiles)
            {
                throw new WarningException("Cannot specify both updateprojectfiles and updateassemblyinfo in the same run. Please rerun GitVersion with only one parameter");
            }
        }

        // Update project files
        var updateProjectFilesResult = parseResult.GetResult(options.UpdateProjectFiles);
        if (updateProjectFilesResult is { Implicit: false })
        {
            arguments.UpdateProjectFiles = true;
            var updateProjectFiles = parseResult.GetValue(options.UpdateProjectFiles);
            if (updateProjectFiles != null)
            {
                foreach (var file in updateProjectFiles)
                {
                    if (!file.Equals("true", StringComparison.OrdinalIgnoreCase) && !file.Equals("1", StringComparison.OrdinalIgnoreCase))
                    {
                        arguments.UpdateAssemblyInfoFileName.Add(file);
                    }
                }
            }

            if (arguments.UpdateAssemblyInfo)
            {
                throw new WarningException("Cannot specify both updateassemblyinfo and updateprojectfiles in the same run. Please rerun GitVersion with only one parameter");
            }

            if (arguments.EnsureAssemblyInfo)
            {
                throw new WarningException("Cannot specify -ensureassemblyinfo with updateprojectfiles: please ensure your project file exists before attempting to update it");
            }
        }

        // Ensure assembly info
        if (parseResult.GetValue(options.EnsureAssemblyInfo))
        {
            arguments.EnsureAssemblyInfo = true;

            if (arguments.UpdateProjectFiles)
            {
                throw new WarningException("Cannot specify -ensureassemblyinfo with updateprojectfiles: please ensure your project file exists before attempting to update it");
            }

            if (arguments.UpdateAssemblyInfoFileName.Count > 1 && arguments.EnsureAssemblyInfo)
            {
                throw new WarningException("Can't specify multiple assembly info files when using /ensureassemblyinfo switch, either use a single assembly info file or do not specify /ensureassemblyinfo and create assembly info files manually");
            }
        }

        // Check assembly info + ensure assembly info cross-validation
        if (arguments.UpdateAssemblyInfoFileName.Count > 1 && arguments.EnsureAssemblyInfo)
        {
            throw new WarningException("Can't specify multiple assembly info files when using /ensureassemblyinfo switch, either use a single assembly info file or do not specify /ensureassemblyinfo and create assembly info files manually");
        }

        // Update wix version file
        if (parseResult.GetValue(options.UpdateWixVersionFile))
            arguments.UpdateWixVersionFile = true;

        // Remote repository args
        var url = parseResult.GetValue(options.Url);
        if (url != null)
            arguments.TargetUrl = url;

        var branch = parseResult.GetValue(options.Branch);
        if (branch != null)
            arguments.TargetBranch = branch;

        var username = parseResult.GetValue(options.Username);
        if (username != null)
            arguments.Authentication.Username = username;

        var password = parseResult.GetValue(options.Password);
        if (password != null)
            arguments.Authentication.Password = password;

        var commit = parseResult.GetValue(options.Commit);
        if (commit != null)
            arguments.CommitId = commit;

        var dynamicRepoLocation = parseResult.GetValue(options.DynamicRepoLocation);
        if (dynamicRepoLocation != null)
            arguments.ClonePath = dynamicRepoLocation;
    }

    private static (RootCommand rootCommand, CommandOptions options) BuildCommand()
    {
        var pathArgument = new Argument<string?>("path")
        {
            Description = "The directory containing .git. If not defined current directory is used.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var versionOption = new Option<bool>("--version")
        {
            Description = "Displays the version of GitVersion"
        };

        var diagnoseOption = new Option<bool>("--diagnose", "-d")
        {
            Description = "Runs GitVersion with additional diagnostic information"
        };

        var logFileOption = new Option<string?>("--log-file", "-l")
        {
            Description = "Path to logfile; specify 'console' to emit to stdout"
        };

        var outputOption = new Option<OutputType[]>("--output", "-o")
        {
            Description = "Determines the output to the console. Can be 'json', 'file', 'buildserver' or 'dotenv'",
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore
        };

        var outputFileOption = new Option<string?>("--output-file")
        {
            Description = "Path to output file. Used in combination with --output 'file'"
        };

        var showVariableOption = new Option<string?>("--show-variable", "-v")
        {
            Description = "Output just a particular variable"
        };

        var formatOption = new Option<string?>("--format", "-f")
        {
            Description = "Output a format containing version variables"
        };

        var configOption = new Option<string?>("--config", "-c")
        {
            Description = "Path to config file (defaults to GitVersion.yml)"
        };

        var showConfigOption = new Option<bool>("--show-config")
        {
            Description = "Outputs the effective GitVersion config in yaml format"
        };

        var overrideConfigOption = new Option<string[]>("--override-config")
        {
            Description = "Overrides GitVersion config values inline (key=value pairs)",
            AllowMultipleArgumentsPerToken = false,
            Arity = ArgumentArity.ZeroOrMore
        };

        var targetPathOption = new Option<string?>("--target-path")
        {
            Description = "Same as 'path', but not positional"
        };

        var noFetchOption = new Option<bool>("--no-fetch")
        {
            Description = "Disables 'git fetch' during version calculation"
        };

        var noCacheOption = new Option<bool>("--no-cache")
        {
            Description = "Bypasses the cache, result will not be written to the cache"
        };

        var noNormalizeOption = new Option<bool>("--no-normalize")
        {
            Description = "Disables normalize step on a build server"
        };

        var allowShallowOption = new Option<bool>("--allow-shallow")
        {
            Description = "Allows GitVersion to run on a shallow clone"
        };

        var verbosityOption = new Option<string?>("--verbosity")
        {
            Description = "Specifies the amount of information to be displayed (Quiet, Minimal, Normal, Verbose, Diagnostic)"
        };

        var updateAssemblyInfoOption = new Option<string[]?>("--update-assembly-info")
        {
            Description = "Will recursively search for all 'AssemblyInfo.cs' files and update them",
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore
        };

        var updateProjectFilesOption = new Option<string[]?>("--update-project-files")
        {
            Description = "Will recursively search for all project files and update them",
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore
        };

        var ensureAssemblyInfoOption = new Option<bool>("--ensure-assembly-info")
        {
            Description = "If the assembly info file specified with --update-assembly-info is not found, it will be created"
        };

        var updateWixVersionFileOption = new Option<bool>("--update-wix-version-file")
        {
            Description = "All the GitVersion variables are written to 'GitVersion_WixVersion.wxi'"
        };

        var urlOption = new Option<string?>("--url")
        {
            Description = "Url to remote git repository"
        };

        var branchOption = new Option<string?>("--branch", "-b")
        {
            Description = "Name of the branch to use on the remote repository"
        };

        var usernameOption = new Option<string?>("--username", "-u")
        {
            Description = "Username in case authentication is required"
        };

        var passwordOption = new Option<string?>("--password", "-p")
        {
            Description = "Password in case authentication is required"
        };

        var commitOption = new Option<string?>("--commit")
        {
            Description = "The commit id to check"
        };

        var dynamicRepoLocationOption = new Option<string?>("--dynamic-repo-location")
        {
            Description = "Override default dynamic repository clone location"
        };

        var rootCommand = new RootCommand("Use convention to derive a SemVer product version from a GitFlow or GitHub based repository.")
        {
            pathArgument, versionOption, diagnoseOption, logFileOption,
            outputOption,
            outputFileOption,
            showVariableOption,
            formatOption,
            configOption,
            showConfigOption,
            overrideConfigOption,
            targetPathOption,
            noFetchOption,
            noCacheOption,
            noNormalizeOption,
            allowShallowOption,
            verbosityOption,
            updateAssemblyInfoOption,
            updateProjectFilesOption,
            ensureAssemblyInfoOption,
            updateWixVersionFileOption,
            urlOption,
            branchOption,
            usernameOption,
            passwordOption,
            commitOption,
            dynamicRepoLocationOption
        };

        var options = new CommandOptions(
            Path: pathArgument,
            Version: versionOption,
            Diagnose: diagnoseOption,
            LogFile: logFileOption,
            Output: outputOption,
            OutputFile: outputFileOption,
            ShowVariable: showVariableOption,
            Format: formatOption,
            Config: configOption,
            ShowConfig: showConfigOption,
            OverrideConfig: overrideConfigOption,
            TargetPath: targetPathOption,
            NoFetch: noFetchOption,
            NoCache: noCacheOption,
            NoNormalize: noNormalizeOption,
            AllowShallow: allowShallowOption,
            VerbosityOption: verbosityOption,
            UpdateAssemblyInfo: updateAssemblyInfoOption,
            UpdateProjectFiles: updateProjectFilesOption,
            EnsureAssemblyInfo: ensureAssemblyInfoOption,
            UpdateWixVersionFile: updateWixVersionFileOption,
            Url: urlOption,
            Branch: branchOption,
            Username: usernameOption,
            Password: passwordOption,
            Commit: commitOption,
            DynamicRepoLocation: dynamicRepoLocationOption
        );

        return (rootCommand, options);
    }

    private void AddAuthentication(Arguments arguments)
    {
        var username = this.environment.GetEnvironmentVariable("GITVERSION_REMOTE_USERNAME");
        if (!username.IsNullOrWhiteSpace())
        {
            arguments.Authentication.Username = username;
        }

        var password = this.environment.GetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD");
        if (!password.IsNullOrWhiteSpace())
        {
            arguments.Authentication.Password = password;
        }
    }

    private IEnumerable<string> ResolveFiles(string workingDirectory, ISet<string>? assemblyInfoFiles)
    {
        if (assemblyInfoFiles == null) yield break;

        foreach (var file in assemblyInfoFiles)
        {
            foreach (var path in this.globbingResolver.Resolve(workingDirectory, file))
            {
                yield return path;
            }
        }
    }

    private void ValidateConfigurationFile(Arguments arguments)
    {
        if (arguments.ConfigurationFile.IsNullOrWhiteSpace()) return;

        if (FileSystemHelper.Path.IsPathRooted(arguments.ConfigurationFile))
        {
            if (!this.fileSystem.File.Exists(arguments.ConfigurationFile))
                throw new WarningException($"Could not find config file at '{arguments.ConfigurationFile}'");
            arguments.ConfigurationFile = FileSystemHelper.Path.GetFullPath(arguments.ConfigurationFile);
        }
        else
        {
            var configFilePath = FileSystemHelper.Path.GetFullPath(
                FileSystemHelper.Path.Combine(arguments.TargetPath, arguments.ConfigurationFile));
            if (!this.fileSystem.File.Exists(configFilePath))
                throw new WarningException($"Could not find config file at '{configFilePath}'");
            arguments.ConfigurationFile = configFilePath;
        }
    }

    private static void ParseShowVariable(Arguments arguments, string value)
    {
        string? versionVariable = null;

        if (!value.IsNullOrWhiteSpace())
        {
            versionVariable = availableVariables.SingleOrDefault(
                av => av.Equals(value.Replace("'", ""), StringComparison.CurrentCultureIgnoreCase));
        }

        if (versionVariable == null)
        {
            var message = $"--show-variable requires a valid version variable. Available variables are:{FileSystemHelper.Path.NewLine}" +
                          string.Join(", ", availableVariables.Select(x => $"'{x}'"));
            throw new WarningException(message);
        }

        arguments.ShowVariable = versionVariable;
    }

    private static void ParseFormat(Arguments arguments, string value)
    {
        if (value.IsNullOrWhiteSpace())
        {
            throw new WarningException("Format requires a valid format string. Available variables are: " +
                                       string.Join(", ", availableVariables));
        }

        var foundVariable = availableVariables.Any(
            variable => value.Contains(variable, StringComparison.CurrentCultureIgnoreCase));

        if (!foundVariable)
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
            return;

        var parser = new OverrideConfigurationOptionParser();

        foreach (var keyValueOption in values)
        {
            var keyAndValue = QuotedStringHelpers.SplitUnquoted(keyValueOption, '=');
            if (keyAndValue.Length != 2)
            {
                throw new WarningException(
                    $"Could not parse /overrideconfig option: {keyValueOption}. Ensure it is in format 'key=value'.");
            }

            var optionKey = keyAndValue[0].ToLowerInvariant();
            if (!OverrideConfigurationOptionParser.SupportedProperties.Contains(optionKey))
            {
                throw new WarningException(
                    $"Could not parse /overrideconfig option: {keyValueOption}. Unsupported 'key'.");
            }

            parser.SetValue(optionKey, keyAndValue[1]);
        }

        arguments.OverrideConfiguration = parser.GetOverrideConfiguration();
    }

    private record CommandOptions(
        Argument<string?> Path,
        Option<bool> Version,
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
        Option<string?> DynamicRepoLocation
    );
}
