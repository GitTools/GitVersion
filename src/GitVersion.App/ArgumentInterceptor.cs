using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Extensions;
using GitVersion.FileSystemGlobbing;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Spectre.Console.Cli;

namespace GitVersion;

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
            arguments.UpdateAssemblyInfoFileName = ResolveFiles(arguments.TargetPath ?? SysEnv.CurrentDirectory, arguments.UpdateAssemblyInfoFileName).ToHashSet();
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
            var parser = new OverrideConfigurationOptionParser();

            foreach (var kvp in settings.OverrideConfiguration)
            {
                // Validate the key format - Spectre.Console.Cli should have already parsed key=value correctly
                // but we still need to validate against supported properties
                var keyValueOption = $"{kvp.Key}={kvp.Value}";

                var optionKey = kvp.Key.ToLowerInvariant();
                if (!OverrideConfigurationOptionParser.SupportedProperties.Contains(optionKey))
                {
                    throw new WarningException($"Could not parse --override-config option: {keyValueOption}. Unsupported 'key'.");
                }

                parser.SetValue(optionKey, kvp.Value);
            }

            arguments.OverrideConfiguration = parser.GetOverrideConfiguration();
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