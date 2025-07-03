using System.ComponentModel;
using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.FileSystemGlobbing;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Spectre.Console.Cli;

namespace GitVersion;

/// <summary>
/// Argument parser that uses Spectre.Console.Cli for enhanced command line processing
/// with POSIX compliant syntax
/// </summary>
internal class SpectreArgumentParser : IArgumentParser
{
    private readonly IEnvironment environment;
    private readonly IFileSystem fileSystem;
    private readonly ICurrentBuildAgent buildAgent;
    private readonly IConsole console;
    private readonly IGlobbingResolver globbingResolver;

    public SpectreArgumentParser(
        IEnvironment environment,
        IFileSystem fileSystem,
        ICurrentBuildAgent buildAgent,
        IConsole console,
        IGlobbingResolver globbingResolver)
    {
        this.environment = environment.NotNull();
        this.fileSystem = fileSystem.NotNull();
        this.buildAgent = buildAgent.NotNull();
        this.console = console.NotNull();
        this.globbingResolver = globbingResolver.NotNull();
    }

    public Arguments ParseArguments(string commandLineArguments)
    {
        var arguments = QuotedStringHelpers.SplitUnquoted(commandLineArguments, ' ');
        return ParseArguments(arguments);
    }

    public Arguments ParseArguments(string[] commandLineArguments)
    {
        // Handle empty arguments
        if (commandLineArguments.Length == 0)
        {
            return CreateDefaultArguments();
        }

        // Handle help requests  
        var firstArg = commandLineArguments[0];
        if (firstArg.IsHelp())
        {
            return new Arguments { IsHelp = true };
        }

        // Handle version requests
        if (firstArg.IsSwitch("version"))
        {
            return new Arguments { IsVersion = true };
        }

        // Use Spectre.Console.Cli to parse arguments
        var app = new CommandApp<GitVersionCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("gitversion");
            config.PropagateExceptions();
        });

        var resultStorage = new ParseResultStorage();
        
        try
        {
            // Parse the arguments
            var interceptor = new ArgumentInterceptor(resultStorage, this.environment, this.fileSystem, this.buildAgent, this.console, this.globbingResolver);
            app.Configure(config => config.Settings.Interceptor = interceptor);

            var parseResult = app.Run(commandLineArguments);
            
            var result = resultStorage.GetResult();
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception)
        {
            // If parsing fails, try to handle legacy forward slash syntax
            return HandleLegacySyntax(commandLineArguments);
        }
        
        return CreateDefaultArguments();
    }

    private Arguments HandleLegacySyntax(string[] commandLineArguments)
    {
        // Convert legacy forward slash syntax to hyphen syntax for Spectre.Console.Cli
        var convertedArgs = new List<string>();
        
        for (int i = 0; i < commandLineArguments.Length; i++)
        {
            var arg = commandLineArguments[i];
            
            // Handle legacy forward slash options
            if (arg.StartsWith('/'))
            {
                var option = arg.Substring(1).ToLowerInvariant();
                switch (option)
                {
                    case "output":
                        convertedArgs.Add("--output");
                        break;
                    case "outputfile":
                        convertedArgs.Add("--output-file");
                        break;
                    case "showvariable":
                        convertedArgs.Add("--show-variable");
                        break;
                    case "format":
                        convertedArgs.Add("--format");
                        break;
                    case "l":
                        convertedArgs.Add("--log-file");
                        break;
                    case "config":
                        convertedArgs.Add("--config");
                        break;
                    case "showconfig":
                        convertedArgs.Add("--show-config");
                        break;
                    case "overrideconfig":
                        convertedArgs.Add("--override-config");
                        // Handle the key=value format for override config
                        if (i + 1 < commandLineArguments.Length)
                        {
                            var nextArg = commandLineArguments[i + 1];
                            if (nextArg.Contains('=') && !nextArg.StartsWith('/') && !nextArg.StartsWith('-'))
                            {
                                // This is the key=value pair for override config
                                convertedArgs.Add(nextArg);
                                i++; // Skip the next argument since we consumed it
                            }
                        }
                        break;
                    case "nocache":
                        convertedArgs.Add("--no-cache");
                        break;
                    case "nonormalize":
                        convertedArgs.Add("--no-normalize");
                        break;
                    case "allowshallow":
                        convertedArgs.Add("--allow-shallow");
                        break;
                    case "verbosity":
                        convertedArgs.Add("--verbosity");
                        break;
                    case "updateassemblyinfo":
                        convertedArgs.Add("--update-assembly-info");
                        break;
                    case "updateprojectfiles":
                        convertedArgs.Add("--update-project-files");
                        break;
                    case "ensureassemblyinfo":
                        convertedArgs.Add("--ensure-assembly-info");
                        break;
                    case "updatewixversionfile":
                        convertedArgs.Add("--update-wix-version-file");
                        break;
                    case "url":
                        convertedArgs.Add("--url");
                        break;
                    case "b":
                        convertedArgs.Add("--branch");
                        break;
                    case "u":
                        convertedArgs.Add("--username");
                        break;
                    case "p":
                        convertedArgs.Add("--password");
                        break;
                    case "c":
                        convertedArgs.Add("--commit");
                        break;
                    case "dynamicrepolocation":
                        convertedArgs.Add("--dynamic-repo-location");
                        break;
                    case "nofetch":
                        convertedArgs.Add("--no-fetch");
                        break;
                    case "targetpath":
                        convertedArgs.Add("--target-path");
                        break;
                    case "diag":
                        convertedArgs.Add("--diag");
                        break;
                    default:
                        // Unknown option, keep as is
                        convertedArgs.Add(arg);
                        break;
                }
            }
            else if (!arg.StartsWith('-') && i == 0)
            {
                // First non-option argument is likely the target path
                convertedArgs.Add(arg);
            }
            else
            {
                // Regular argument or already in correct format
                convertedArgs.Add(arg);
            }
        }
        
        // Try parsing again with converted arguments
        try
        {
            var app = new CommandApp<GitVersionCommand>();
            app.Configure(config =>
            {
                config.SetApplicationName("gitversion");
                config.PropagateExceptions();
            });

            var resultStorage = new ParseResultStorage();
            var interceptor = new ArgumentInterceptor(resultStorage, this.environment, this.fileSystem, this.buildAgent, this.console, this.globbingResolver);
            app.Configure(config => config.Settings.Interceptor = interceptor);

            var parseResult = app.Run(convertedArgs.ToArray());
            
            var result = resultStorage.GetResult();
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception)
        {
            // Final fallback - if it's a single argument and not an option, treat as target path
            if (commandLineArguments.Length == 1 && !commandLineArguments[0].StartsWith('-') && !commandLineArguments[0].StartsWith('/'))
            {
                return CreateArgumentsWithTargetPath(commandLineArguments[0]);
            }
        }
        
        return CreateDefaultArguments();
    }

    private Arguments CreateDefaultArguments()
    {
        var args = new Arguments
        {
            TargetPath = SysEnv.CurrentDirectory
        };
        args.Output.Add(OutputType.Json);
        AddAuthentication(args);
        args.NoFetch = this.buildAgent.PreventFetch();
        return args;
    }

    private Arguments CreateArgumentsWithTargetPath(string targetPath)
    {
        var args = new Arguments
        {
            TargetPath = targetPath.TrimEnd('/', '\\')
        };
        args.Output.Add(OutputType.Json);
        AddAuthentication(args);
        args.NoFetch = this.buildAgent.PreventFetch();
        return args;
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
}

/// <summary>
/// Storage for parse results
/// </summary>
internal class ParseResultStorage
{
    private Arguments? result;

    public void SetResult(Arguments arguments) => this.result = arguments;
    public Arguments? GetResult() => this.result;
}

/// <summary>
/// Interceptor to capture parsed arguments
/// </summary>
internal class ArgumentInterceptor : ICommandInterceptor
{
    private readonly ParseResultStorage storage;
    private readonly IEnvironment environment;
    private readonly IFileSystem fileSystem;
    private readonly ICurrentBuildAgent buildAgent;
    private readonly IConsole console;
    private readonly IGlobbingResolver globbingResolver;

    public ArgumentInterceptor(ParseResultStorage storage, IEnvironment environment, IFileSystem fileSystem, ICurrentBuildAgent buildAgent, IConsole console, IGlobbingResolver globbingResolver)
    {
        this.storage = storage;
        this.environment = environment;
        this.fileSystem = fileSystem;
        this.buildAgent = buildAgent;
        this.console = console;
        this.globbingResolver = globbingResolver;
    }

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is GitVersionSettings gitVersionSettings)
        {
            var arguments = ConvertToArguments(gitVersionSettings);
            AddAuthentication(arguments);
            ValidateAndProcessArguments(arguments);
            this.storage.SetResult(arguments);
        }
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

    private void ValidateAndProcessArguments(Arguments arguments)
    {
        // Apply default output if none specified
        if (arguments.Output.Count == 0)
        {
            arguments.Output.Add(OutputType.Json);
        }

        // Set default output file if file output is specified
        if (arguments.Output.Contains(OutputType.File) && arguments.OutputFile == null)
        {
            arguments.OutputFile = "GitVersion.json";
        }

        // Apply build agent settings
        arguments.NoFetch = arguments.NoFetch || this.buildAgent.PreventFetch();

        // Validate configuration file
        ValidateConfigurationFile(arguments);

        // Process assembly info files
        if (!arguments.EnsureAssemblyInfo)
        {
            arguments.UpdateAssemblyInfoFileName = ResolveFiles(arguments.TargetPath, arguments.UpdateAssemblyInfoFileName).ToHashSet();
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
            var configFilePath = FileSystemHelper.Path.GetFullPath(FileSystemHelper.Path.Combine(arguments.TargetPath, arguments.ConfigurationFile));
            if (!this.fileSystem.File.Exists(configFilePath))
                throw new WarningException($"Could not find config file at '{configFilePath}'");
            arguments.ConfigurationFile = configFilePath;
        }
    }

    private IEnumerable<string> ResolveFiles(string workingDirectory, ISet<string>? assemblyInfoFiles)
    {
        if (assemblyInfoFiles == null || assemblyInfoFiles.Count == 0)
        {
            return [];
        }

        var stringList = new List<string>();

        foreach (var filePattern in assemblyInfoFiles)
        {
            if (FileSystemHelper.Path.IsPathRooted(filePattern))
            {
                stringList.Add(filePattern);
            }
            else
            {
                var searchRoot = FileSystemHelper.Path.GetFullPath(workingDirectory);
                var matchingFiles = this.globbingResolver.Resolve(searchRoot, filePattern);
                stringList.AddRange(matchingFiles);
            }
        }

        return stringList;
    }

    private static Arguments ConvertToArguments(GitVersionSettings settings)
    {
        var arguments = new Arguments();

        // Set target path - prioritize explicit targetpath option over positional argument
        arguments.TargetPath = settings.TargetPathOption?.TrimEnd('/', '\\') 
                              ?? settings.TargetPath?.TrimEnd('/', '\\') 
                              ?? SysEnv.CurrentDirectory;

        // Configuration options
        arguments.ConfigurationFile = settings.ConfigurationFile;
        arguments.ShowConfiguration = settings.ShowConfiguration;
        
        // Handle override configuration
        if (settings.OverrideConfiguration != null && settings.OverrideConfiguration.Any())
        {
            var overrideConfig = new Dictionary<object, object?>();
            foreach (var kvp in settings.OverrideConfiguration)
            {
                overrideConfig[kvp.Key] = kvp.Value;
            }
            arguments.OverrideConfiguration = overrideConfig;
        }
        else
        {
            arguments.OverrideConfiguration = new Dictionary<object, object?>();
        }
        
        // Output options
        if (settings.Output != null && settings.Output.Any())
        {
            foreach (var output in settings.Output)
            {
                if (Enum.TryParse<OutputType>(output, true, out var outputType))
                {
                    arguments.Output.Add(outputType);
                }
            }
        }

        arguments.OutputFile = settings.OutputFile;
        arguments.Format = settings.Format;
        arguments.ShowVariable = settings.ShowVariable;

        // Repository options  
        arguments.TargetUrl = settings.Url;
        arguments.TargetBranch = settings.Branch;
        arguments.CommitId = settings.Commit;
        arguments.ClonePath = settings.DynamicRepoLocation;

        // Authentication
        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            arguments.Authentication.Username = settings.Username;
        }
        if (!string.IsNullOrWhiteSpace(settings.Password))
        {
            arguments.Authentication.Password = settings.Password;
        }

        // Behavioral flags
        arguments.NoFetch = settings.NoFetch;
        arguments.NoCache = settings.NoCache;
        arguments.NoNormalize = settings.NoNormalize;
        arguments.AllowShallow = settings.AllowShallow;
        arguments.Diag = settings.Diag;

        // Assembly info options
        arguments.UpdateAssemblyInfo = settings.UpdateAssemblyInfo;
        arguments.EnsureAssemblyInfo = settings.EnsureAssemblyInfo;
        arguments.UpdateProjectFiles = settings.UpdateProjectFiles;
        arguments.UpdateWixVersionFile = settings.UpdateWixVersionFile;

        // Handle assembly info file names
        if (settings.UpdateAssemblyInfoFileName != null && settings.UpdateAssemblyInfoFileName.Any())
        {
            arguments.UpdateAssemblyInfoFileName = settings.UpdateAssemblyInfoFileName.ToHashSet();
        }

        // Logging
        arguments.LogFilePath = settings.LogFilePath;
        if (Enum.TryParse<Verbosity>(settings.Verbosity, true, out var verbosity))
        {
            arguments.Verbosity = verbosity;
        }

        return arguments;
    }
}

/// <summary>
/// Main GitVersion command with POSIX compliant options
/// </summary>
[Description("Generate version information based on Git repository")]
internal class GitVersionCommand : Command<GitVersionSettings>
{
    public override int Execute(CommandContext context, GitVersionSettings settings)
    {
        // The actual logic is handled by the interceptor
        // This just returns success to continue normal flow
        return 0;
    }
}

/// <summary>
/// Settings class for Spectre.Console.Cli with POSIX compliant options
/// </summary>
internal class GitVersionSettings : CommandSettings
{
    [CommandArgument(0, "[path]")]
    [Description("Path to the Git repository (defaults to current directory)")]
    public string? TargetPath { get; set; }

    [CommandOption("--config")]
    [Description("Path to GitVersion configuration file")]
    public string? ConfigurationFile { get; set; }

    [CommandOption("--show-config")]
    [Description("Display the effective GitVersion configuration and exit")]
    public bool ShowConfiguration { get; set; }

    [CommandOption("--override-config")]
    [Description("Override GitVersion configuration values")]
    public Dictionary<string, string>? OverrideConfiguration { get; set; }

    [CommandOption("-o|--output")]
    [Description("Output format (json, file, buildserver, console)")]
    public string[]? Output { get; set; }

    [CommandOption("--output-file")]
    [Description("Output file when using file output")]  
    public string? OutputFile { get; set; }

    [CommandOption("-f|--format")]
    [Description("Format string for version output")]
    public string? Format { get; set; }

    [CommandOption("-v|--show-variable")]
    [Description("Show a specific GitVersion variable")]
    public string? ShowVariable { get; set; }

    [CommandOption("--url")]
    [Description("Remote repository URL")]
    public string? Url { get; set; }

    [CommandOption("-b|--branch")]
    [Description("Target branch name")]
    public string? Branch { get; set; }

    [CommandOption("-c|--commit")]
    [Description("Target commit SHA")]
    public string? Commit { get; set; }

    [CommandOption("--target-path")]
    [Description("Same as positional path argument")]
    public string? TargetPathOption { get; set; }

    [CommandOption("--dynamic-repo-location")]
    [Description("Path to clone remote repository")]
    public string? DynamicRepoLocation { get; set; }

    [CommandOption("-u|--username")]
    [Description("Username for remote repository authentication")]
    public string? Username { get; set; }

    [CommandOption("-p|--password")]
    [Description("Password for remote repository authentication")]
    public string? Password { get; set; }

    [CommandOption("--no-fetch")]
    [Description("Disable Git fetch")]
    public bool NoFetch { get; set; }

    [CommandOption("--no-cache")]
    [Description("Disable GitVersion result caching")]
    public bool NoCache { get; set; }

    [CommandOption("--no-normalize")]
    [Description("Disable branch name normalization")]
    public bool NoNormalize { get; set; }

    [CommandOption("--allow-shallow")]
    [Description("Allow operation on shallow Git repositories")]
    public bool AllowShallow { get; set; }

    [CommandOption("--diag")]
    [Description("Enable diagnostic output")]
    public bool Diag { get; set; }

    [CommandOption("--update-assembly-info")]
    [Description("Update AssemblyInfo files")]
    public bool UpdateAssemblyInfo { get; set; }

    [CommandOption("--ensure-assembly-info")]
    [Description("Ensure AssemblyInfo files exist")]
    public bool EnsureAssemblyInfo { get; set; }

    [CommandOption("--update-assembly-info-filename")]
    [Description("Specific AssemblyInfo files to update")]
    public string[]? UpdateAssemblyInfoFileName { get; set; }

    [CommandOption("--update-project-files")]
    [Description("Update MSBuild project files")]
    public bool UpdateProjectFiles { get; set; }

    [CommandOption("--update-wix-version-file")]
    [Description("Update WiX version file")]
    public bool UpdateWixVersionFile { get; set; }

    [CommandOption("-l|--log-file")]
    [Description("Path to log file")]
    public string? LogFilePath { get; set; }

    [CommandOption("--verbosity")]
    [Description("Logging verbosity (quiet, minimal, normal, verbose, diagnostic)")]
    public string? Verbosity { get; set; }
}