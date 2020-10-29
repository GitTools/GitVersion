using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model;
using GitVersion.Model.Configuration;
using GitVersion.OutputVariables;

namespace GitVersion
{
    public class ArgumentParser : IArgumentParser
    {
        private readonly IEnvironment environment;
        private readonly ICurrentBuildAgent buildAgent;
        private readonly IConsole console;
        private readonly IGlobbingResolver globbingResolver;
        private const string defaultOutputFileName = "GitVersion.json";

        public ArgumentParser(IEnvironment environment, ICurrentBuildAgent buildAgent, IConsole console, IGlobbingResolver globbingResolver)
        {
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.globbingResolver = globbingResolver ?? throw new ArgumentNullException(nameof(globbingResolver));
            this.buildAgent = buildAgent;
        }

        public Arguments ParseArguments(string commandLineArguments)
        {
            var arguments = commandLineArguments
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            return ParseArguments(arguments);
        }

        public Arguments ParseArguments(string[] commandLineArguments)
        {
            if (commandLineArguments.Length == 0)
            {
                var args = new Arguments
                {
                    TargetPath = System.Environment.CurrentDirectory,
                };

                args.Output.Add(OutputType.Json);

                AddAuthentication(args);

                args.NoFetch = buildAgent != null && buildAgent.PreventFetch();

                return args;
            }

            var firstArgument = commandLineArguments.First();

            if (firstArgument.IsInit())
            {
                return new Arguments
                {
                    TargetPath = System.Environment.CurrentDirectory,
                    Init = true,
                };
            }

            if (firstArgument.IsHelp())
            {
                return new Arguments
                {
                    IsHelp = true,
                };
            }

            if (firstArgument.IsSwitch("version"))
            {
                return new Arguments
                {
                    IsVersion = true,
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
                ? System.Environment.CurrentDirectory
                : firstArgument;

            arguments.TargetPath = arguments.TargetPath.TrimEnd('/', '\\');

            if (!arguments.EnsureAssemblyInfo) arguments.UpdateAssemblyInfoFileName = ResolveFiles(arguments.TargetPath, arguments.UpdateAssemblyInfoFileName).ToHashSet();
            arguments.NoFetch = arguments.NoFetch || buildAgent != null && buildAgent.PreventFetch();

            return arguments;
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
            var username = environment.GetEnvironmentVariable("GITVERSION_REMOTE_USERNAME");
            if (!string.IsNullOrWhiteSpace(username))
            {
                arguments.Authentication.Username = username;
            }

            var password = environment.GetEnvironmentVariable("GITVERSION_REMOTE_PASSWORD");
            if (!string.IsNullOrWhiteSpace(password))
            {
                arguments.Authentication.Password = password;
            }
        }

        private IEnumerable<string> ResolveFiles(string workingDirectory, ISet<string> assemblyInfoFiles)
        {
            if (assemblyInfoFiles == null) yield break;

            foreach (var file in assemblyInfoFiles)
            {
                var paths = globbingResolver.Resolve(workingDirectory, file);

                foreach (var path in paths)
                {
                    yield return Path.GetFullPath(Path.Combine(workingDirectory, path));
                }
            }
        }

        private void ParseTargetPath(Arguments arguments, string name, string[] values, string value, bool parseEnded)
        {
            if (name.IsSwitch("targetpath"))
            {
                EnsureArgumentValueCount(values);
                arguments.TargetPath = value;
                if (!Directory.Exists(value))
                {
                    console.WriteLine($"The working directory '{value}' does not exist.");
                }

                return;
            }

            var couldNotParseMessage = $"Could not parse command line parameter '{name}'.";

            // If we've reached through all argument switches without a match, we can relatively safely assume that the first argument isn't a switch, but the target path.
            if (parseEnded)
            {
                if (name.StartsWith("/"))
                {
                    if (Path.DirectorySeparatorChar == '/' && name.IsValidPath())
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
            }

            throw new WarningException(couldNotParseMessage);
        }

        private static bool ParseSwitches(Arguments arguments, string name, string[] values, string value)
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
                if (value == null || value.IsTrue())
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

            if (name.IsSwitch("verbosity"))
            {
                ParseVerbosity(arguments, value);
                return true;
            }

            if (name.IsSwitch("updatewixversionfile"))
            {
                arguments.UpdateWixVersionFile = true;
                return true;
            }

            return false;
        }

        private static bool ParseConfigArguments(Arguments arguments, string name, string[] values, string value)
        {
            if (name.IsSwitch("config"))
            {
                EnsureArgumentValueCount(values);
                arguments.ConfigFile = value;
                return true;
            }

            if (name.IsSwitch("overrideconfig"))
            {
                ParseOverrideConfig(arguments, value);
                return true;
            }

            if (name.IsSwitch("showConfig"))
            {
                arguments.ShowConfig = value.IsTrue() || !value.IsFalse();
                return true;
            }

            return false;
        }

        private static bool ParseRemoteArguments(Arguments arguments, string name, string[] values, string value)
        {
            if (name.IsSwitch("dynamicRepoLocation"))
            {
                EnsureArgumentValueCount(values);
                arguments.DynamicRepositoryClonePath = value;
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

            if (name.IsSwitch("b"))
            {
                EnsureArgumentValueCount(values);
                arguments.TargetBranch = value;
                return true;
            }

            return false;
        }

        private static void ParseShowVariable(Arguments arguments, string value, string name)
        {
            string versionVariable = null;

            if (!string.IsNullOrWhiteSpace(value))
            {
                versionVariable = VersionVariables.AvailableVariables.SingleOrDefault(av => av.Equals(value.Replace("'", ""), StringComparison.CurrentCultureIgnoreCase));
            }

            if (versionVariable == null)
            {
                var message = $"{name} requires a valid version variable. Available variables are:{System.Environment.NewLine}" +
                              string.Join(", ", VersionVariables.AvailableVariables.Select(x => string.Concat("'", x, "'")));
                throw new WarningException(message);
            }

            arguments.ShowVariable = versionVariable;
        }

        private static void ParseEnsureAssemblyInfo(Arguments arguments, string value)
        {
            arguments.EnsureAssemblyInfo = true;
            if (value.IsFalse())
            {
                arguments.EnsureAssemblyInfo = false;
            }

            if (arguments.UpdateProjectFiles)
            {
                throw new WarningException("Cannot specify -ensureassemblyinfo with updateprojectfiles: please ensure your project file exists before attempting to update it");
            }

            if (arguments.UpdateAssemblyInfoFileName.Count > 1 && arguments.EnsureAssemblyInfo)
            {
                throw new WarningException("Can't specify multiple assembly info files when using /ensureassemblyinfo switch, either use a single assembly info file or do not specify /ensureassemblyinfo and create assembly info files manually");
            }
        }

        private static void ParseOutput(Arguments arguments, string[] values)
        {
            foreach (var v in values)
            {
                if (!Enum.TryParse(v, true, out OutputType outputType))
                {
                    throw new WarningException($"Value '{v}' cannot be parsed as output type, please use 'json', 'file' or 'buildserver'");
                }

                arguments.Output.Add(outputType);
            }
        }

        private static void ParseVerbosity(Arguments arguments, string value)
        {
            // first try the old version, this check will be removed in version 6.0.0, making it a breaking change
            if (Enum.TryParse(value, true, out LogLevel logLevel))
            {
                arguments.Verbosity = LogExtensions.GetVerbosityForLevel(logLevel);
            }
            else if (!Enum.TryParse(value, true, out arguments.Verbosity))
            {
                throw new WarningException($"Could not parse Verbosity value '{value}'");
            }
        }

        private static void ParseOverrideConfig(Arguments arguments, string value)
        {
            var keyValueOptions = (value ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (keyValueOptions.Length == 0)
            {
                return;
            }

            arguments.OverrideConfig = new Config();

            if (keyValueOptions.Length > 1)
            {
                throw new WarningException("Can't specify multiple /overrideconfig options: currently supported only 'tag-prefix' option");
            }

            // key=value
            foreach (var keyValueOption in keyValueOptions)
            {
                var keyAndValue = keyValueOption.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (keyAndValue.Length != 2)
                {
                    throw new WarningException($"Could not parse /overrideconfig option: {keyValueOption}. Ensure it is in format 'key=value'");
                }

                var optionKey = keyAndValue[0].ToLowerInvariant();
                arguments.OverrideConfig.TagPrefix = optionKey switch
                {
                    "tag-prefix" => keyAndValue[1],
                    _ => throw new WarningException($"Could not parse /overrideconfig option: {optionKey}. Currently supported only 'tag-prefix' option")
                };
            }
        }

        private static void ParseUpdateAssemblyInfo(Arguments arguments, string value, string[] values)
        {
            if (value.IsTrue())
            {
                arguments.UpdateAssemblyInfo = true;
            }
            else if (value.IsFalse())
            {
                arguments.UpdateAssemblyInfo = false;
            }
            else if (values != null && values.Length > 1)
            {
                arguments.UpdateAssemblyInfo = true;
                foreach (var v in values)
                {
                    arguments.UpdateAssemblyInfoFileName.Add(v);
                }
            }
            else if (!value.IsSwitchArgument())
            {
                arguments.UpdateAssemblyInfo = true;
                if (value != null)
                {
                    arguments.UpdateAssemblyInfoFileName.Add(value);
                }
            }
            else
            {
                arguments.UpdateAssemblyInfo = true;
            }

            if (arguments.UpdateProjectFiles)
            {
                throw new WarningException("Cannot specify both updateprojectfiles and updateassemblyinfo in the same run. Please rerun GitVersion with only one parameter");
            }
            if (arguments.UpdateAssemblyInfoFileName.Count > 1 && arguments.EnsureAssemblyInfo)
            {
                throw new WarningException("Can't specify multiple assembly info files when using -ensureassemblyinfo switch, either use a single assembly info file or do not specify -ensureassemblyinfo and create assembly info files manually");
            }
        }

        private static void ParseUpdateProjectInfo(Arguments arguments, string value, string[] values)
        {
            if (value.IsTrue())
            {
                arguments.UpdateProjectFiles = true;
            }
            else if (value.IsFalse())
            {
                arguments.UpdateProjectFiles = false;
            }
            else if (values != null && values.Length > 1)
            {
                arguments.UpdateProjectFiles = true;
                foreach (var v in values)
                {
                    arguments.UpdateAssemblyInfoFileName.Add(v);
                }
            }
            else if (!value.IsSwitchArgument())
            {
                arguments.UpdateProjectFiles = true;
                if (value != null)
                {
                    arguments.UpdateAssemblyInfoFileName.Add(value);
                }
            }
            else
            {
                arguments.UpdateProjectFiles = true;
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

        private static void EnsureArgumentValueCount(IReadOnlyList<string> values)
        {
            if (values != null && values.Count > 1)
            {
                throw new WarningException($"Could not parse command line parameter '{values[1]}'.");
            }
        }

        private static NameValueCollection CollectSwitchesAndValuesFromArguments(IList<string> namedArguments, out bool firstArgumentIsSwitch)
        {
            firstArgumentIsSwitch = true;
            var switchesAndValues = new NameValueCollection();
            string currentKey = null;
            var argumentRequiresValue = false;

            for (var i = 0; i < namedArguments.Count; i += 1)
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
                    if (string.IsNullOrEmpty(switchesAndValues[currentKey]))
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
}
