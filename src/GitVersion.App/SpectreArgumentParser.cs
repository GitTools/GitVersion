using System.Collections.Specialized;
using System.IO.Abstractions;
using GitVersion.Agents;
using GitVersion.Extensions;
using GitVersion.FileSystemGlobbing;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using Spectre.Console.Cli;

namespace GitVersion;

internal class SpectreArgumentParser : IArgumentParser
{
    private readonly IEnvironment environment;
    private readonly IFileSystem fileSystem;
    private readonly ICurrentBuildAgent buildAgent;
    private readonly IConsole console;
    private readonly IGlobbingResolver globbingResolver;

    private const string defaultOutputFileName = "GitVersion.json";
    private static readonly IEnumerable<string> availableVariables = GitVersionVariables.AvailableVariables;

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
        if (commandLineArguments.Length == 0)
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

        var firstArgument = commandLineArguments[0];

        if (firstArgument.IsHelp())
        {
            // Use Spectre.Console.Cli for help generation
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.SetApplicationName("gitversion");
                config.SetApplicationVersion("1.0.0");
            });
            // Note: We'd need to define a proper command structure here for full help support
            // For now, return help flag to maintain existing behavior
            return new Arguments
            {
                IsHelp = true
            };
        }

        if (firstArgument.IsSwitch("version"))
        {
            return new Arguments
            {
                IsVersion = true
            };
        }

        var arguments = new Arguments();
        AddAuthentication(arguments);

        var switchesAndValues = CollectSwitchesAndValuesFromArguments(commandLineArguments, out var firstArgumentIsSwitch);

        for (var i = 0; i < switchesAndValues.AllKeys.Length; i++)
        {
            ParseSwitchArguments(arguments, switchesAndValues, i);
        }

        if (arguments.Output.Count == 0)
        {
            arguments.Output.Add(OutputType.Json);
        }

        if (arguments.Output.Contains(OutputType.File) && arguments.OutputFile == null)
        {
            arguments.OutputFile = defaultOutputFileName;
        }

        // If the first argument is a switch, it should already have been consumed in the above loop,
        // or else a WarningException should have been thrown and we wouldn't end up here.
        arguments.TargetPath ??= firstArgumentIsSwitch
            ? SysEnv.CurrentDirectory
            : firstArgument;

        arguments.TargetPath = arguments.TargetPath.TrimEnd('/', '\\');

        if (!arguments.EnsureAssemblyInfo) arguments.UpdateAssemblyInfoFileName = ResolveFiles(arguments.TargetPath, arguments.UpdateAssemblyInfoFileName).ToHashSet();
        arguments.NoFetch = arguments.NoFetch || this.buildAgent.PreventFetch();

        ValidateConfigurationFile(arguments);

        return arguments;
    }

    // Copy all the original parser methods with minimal changes
    private void ValidateConfigurationFile(Arguments arguments)
    {
        if (arguments.ConfigurationFile.IsNullOrWhiteSpace()) return;

        if (FileSystemHelper.Path.IsPathRooted(arguments.ConfigurationFile))
        {
            if (!this.fileSystem.File.Exists(arguments.ConfigurationFile)) throw new WarningException($"Could not find config file at '{arguments.ConfigurationFile}'");
            arguments.ConfigurationFile = FileSystemHelper.Path.GetFullPath(arguments.ConfigurationFile);
        }
        else
        {
            var configFilePath = FileSystemHelper.Path.GetFullPath(FileSystemHelper.Path.Combine(arguments.TargetPath, arguments.ConfigurationFile));
            if (!this.fileSystem.File.Exists(configFilePath)) throw new WarningException($"Could not find config file at '{configFilePath}'");
            arguments.ConfigurationFile = configFilePath;
        }
    }

    private void ParseSwitchArguments(Arguments arguments, NameValueCollection switchesAndValues, int i)
    {
        var name = switchesAndValues.AllKeys[i];
        var values = switchesAndValues.GetValues(name);
        var value = values?.FirstOrDefault();

        if (ParseSwitches(arguments, name, values, value)) return;

        ParseTargetPath(arguments, name, values, value, i == 0);
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

    private void ParseTargetPath(Arguments arguments, string? name, IReadOnlyList<string>? values, string? value, bool parseEnded)
    {
        if (name.IsSwitch("targetpath"))
        {
            EnsureArgumentValueCount(values);
            arguments.TargetPath = value;
            if (string.IsNullOrWhiteSpace(value) || !this.fileSystem.Directory.Exists(value))
            {
                this.console.WriteLine($"The working directory '{value}' does not exist.");
            }

            return;
        }

        var couldNotParseMessage = $"Could not parse command line parameter '{name}'.";

        // If we've reached through all argument switches without a match, we can relatively safely assume that the first argument isn't a switch, but the target path.
        if (!parseEnded) throw new WarningException(couldNotParseMessage);
        if (name?.StartsWith('/') == true)
        {
            if (FileSystemHelper.Path.DirectorySeparatorChar == '/' && name.IsValidPath())
            {
                arguments.TargetPath = name;
                return;
            }
        }
        else if (!name.IsSwitchArgument())
        {
            arguments.TargetPath = name;
            return;
        }

        couldNotParseMessage += " If it is the target path, make sure it exists.";

        throw new WarningException(couldNotParseMessage);
    }

    private static bool ParseSwitches(Arguments arguments, string? name, IReadOnlyList<string>? values, string? value)
    {
        if (name.IsSwitch("l"))
        {
            EnsureArgumentValueCount(values);
            arguments.LogFilePath = value;
            return true;
        }

        if (ParseConfigArguments(arguments, name, values, value)) return true;

        if (ParseRemoteArguments(arguments, name, values, value)) return true;

        if (name.IsSwitch("diag"))
        {
            if (value?.IsTrue() != false)
            {
                arguments.Diag = true;
            }

            return true;
        }

        if (name.IsSwitch("updateprojectfiles"))
        {
            ParseUpdateProjectInfo(arguments, value, values);
            return true;
        }

        if (name.IsSwitch("updateAssemblyInfo"))
        {
            ParseUpdateAssemblyInfo(arguments, value, values);
            return true;
        }

        if (name.IsSwitch("ensureassemblyinfo"))
        {
            ParseEnsureAssemblyInfo(arguments, value);
            return true;
        }

        if (name.IsSwitch("v") || name.IsSwitch("showvariable"))
        {
            ParseShowVariable(arguments, value, name);
            return true;
        }

        if (name.IsSwitch("format"))
        {
            ParseFormat(arguments, value);
            return true;
        }

        if (name.IsSwitch("output"))
        {
            ParseOutput(arguments, values);
            return true;
        }

        if (name.IsSwitch("outputfile"))
        {
            EnsureArgumentValueCount(values);
            arguments.OutputFile = value;
            return true;
        }

        if (name.IsSwitch("nofetch"))
        {
            arguments.NoFetch = true;
            return true;
        }

        if (name.IsSwitch("nonormalize"))
        {
            arguments.NoNormalize = true;
            return true;
        }

        if (name.IsSwitch("nocache"))
        {
            arguments.NoCache = true;
            return true;
        }

        if (name.IsSwitch("allowshallow"))
        {
            arguments.AllowShallow = true;
            return true;
        }

        if (name.IsSwitch("verbosity"))
        {
            ParseVerbosity(arguments, value);
            return true;
        }

        if (!name.IsSwitch("updatewixversionfile")) return false;
        arguments.UpdateWixVersionFile = true;
        return true;
    }

    private static bool ParseConfigArguments(Arguments arguments, string? name, IReadOnlyList<string>? values, string? value)
    {
        if (name.IsSwitch("config"))
        {
            EnsureArgumentValueCount(values);
            arguments.ConfigurationFile = value;
            return true;
        }

        if (name.IsSwitch("overrideconfig"))
        {
            ParseOverrideConfig(arguments, values);
            return true;
        }

        if (!name.IsSwitch("showConfig"))
            return false;

        arguments.ShowConfiguration = value.IsTrue() || !value.IsFalse();
        return true;
    }

    private static bool ParseRemoteArguments(Arguments arguments, string? name, IReadOnlyList<string>? values, string? value)
    {
        if (name.IsSwitch("dynamicRepoLocation"))
        {
            EnsureArgumentValueCount(values);
            arguments.ClonePath = value;
            return true;
        }

        if (name.IsSwitch("url"))
        {
            EnsureArgumentValueCount(values);
            arguments.TargetUrl = value;
            return true;
        }

        if (name.IsSwitch("u"))
        {
            EnsureArgumentValueCount(values);
            arguments.Authentication.Username = value;
            return true;
        }

        if (name.IsSwitch("p"))
        {
            EnsureArgumentValueCount(values);
            arguments.Authentication.Password = value;
            return true;
        }

        if (name.IsSwitch("c"))
        {
            EnsureArgumentValueCount(values);
            arguments.CommitId = value;
            return true;
        }

        if (!name.IsSwitch("b")) return false;
        EnsureArgumentValueCount(values);
        arguments.TargetBranch = value;
        return true;
    }

    private static void ParseShowVariable(Arguments arguments, string? value, string? name)
    {
        if (value == null)
        {
            throw new WarningException($"Missing argument for switch `{name}`, expected a variable name. Available variables: {string.Join(", ", availableVariables)}");
        }

        arguments.ShowVariable = value;
    }

    private static void ParseFormat(Arguments arguments, string? value)
    {
        if (value == null)
        {
            throw new WarningException("Missing argument for switch `/format`, expected an assembly format pattern like `{SemVer}`. See documentation for more information.");
        }

        arguments.Format = value;
    }

    private static void ParseEnsureAssemblyInfo(Arguments arguments, string? value)
    {
        arguments.EnsureAssemblyInfo = value?.IsTrue() != false;
    }

    private static void ParseOutput(Arguments arguments, IEnumerable<string>? values)
    {
        if (values == null)
            return;

        foreach (var v in values)
        {
            if (!Enum.TryParse(v, true, out OutputType outputType))
            {
                throw new WarningException($"Value '{v}' cannot be parsed as output type, please use 'json', 'file', 'buildserver' or 'dotenv'");
            }

            arguments.Output.Add(outputType);
        }
    }

    private static void ParseVerbosity(Arguments arguments, string? value)
    {
        if (!Enum.TryParse(value, true, out arguments.Verbosity))
        {
            throw new WarningException($"Could not parse Verbosity value '{value}'");
        }
    }

    private static void ParseOverrideConfig(Arguments arguments, IReadOnlyCollection<string>? values)
    {
        if (values == null) return;

        var overrideConfig = new Dictionary<object, object?>();
        foreach (var config in values)
        {
            var keyValue = config.Split('=', 2);
            if (keyValue.Length != 2)
            {
                throw new WarningException($"Could not parse /overrideconfig option: {config}. Ensure it is in format 'key=value'.");
            }
            overrideConfig[keyValue[0]] = keyValue[1];
        }
        arguments.OverrideConfiguration = overrideConfig;
    }

    private static void ParseUpdateAssemblyInfo(Arguments arguments, string? value, IReadOnlyCollection<string>? values)
    {
        arguments.UpdateAssemblyInfo = true;

        if (values == null) return;

        if (value != null)
        {
            if (!value.IsSwitchArgument())
            {
                arguments.UpdateAssemblyInfoFileName.Add(value);
            }
        }
        else
        {
            foreach (var v in values)
            {
                if (!v.IsSwitchArgument())
                {
                    arguments.UpdateAssemblyInfoFileName.Add(v);
                }
            }
        }
    }

    private static void ParseUpdateProjectInfo(Arguments arguments, string? value, IReadOnlyCollection<string>? values)
    {
        arguments.UpdateProjectFiles = true;

        if (values == null) return;

        if (value != null)
        {
            if (!value.IsSwitchArgument())
            {
                arguments.UpdateAssemblyInfoFileName.Add(value);
            }
        }
        else
        {
            foreach (var v in values)
            {
                if (!v.IsSwitchArgument())
                {
                    arguments.UpdateAssemblyInfoFileName.Add(v);
                }
            }
        }
    }

    private static void EnsureArgumentValueCount(IReadOnlyCollection<string>? values)
    {
        if (values is { Count: > 1 })
        {
            throw new WarningException($"Could not parse command line parameter '{string.Join(", ", values)}'.");
        }
    }

    private static NameValueCollection CollectSwitchesAndValuesFromArguments(string[] namedArguments, out bool firstArgumentIsSwitch)
    {
        firstArgumentIsSwitch = true;
        var switchesAndValues = new NameValueCollection();
        string? currentKey = null;
        var argumentRequiresValue = false;

        for (var i = 0; i < namedArguments.Length; ++i)
        {
            var arg = namedArguments[i];

            // If the current (previous) argument doesn't require a value parameter and this is a switch, create new name/value entry for it, with a null value.
            if (!argumentRequiresValue && arg.IsSwitchArgument())
            {
                currentKey = arg;
                argumentRequiresValue = arg.ArgumentRequiresValue(i);
                switchesAndValues.Add(currentKey, null);
            }
            // If this is a value (not a switch)
            else if (currentKey != null)
            {
                // And if the current switch does not have a value yet and the value is not itself a switch, set its value to this argument.
                if (switchesAndValues[currentKey].IsNullOrEmpty())
                {
                    switchesAndValues[currentKey] = arg;
                }
                // Otherwise add the value under the same switch.
                else
                {
                    switchesAndValues.Add(currentKey, arg);
                }

                // Reset the boolean argument flag so the next argument won't be ignored.
                argumentRequiresValue = false;
            }
            else if (i == 0)
            {
                firstArgumentIsSwitch = false;
            }
        }

        return switchesAndValues;
    }
}