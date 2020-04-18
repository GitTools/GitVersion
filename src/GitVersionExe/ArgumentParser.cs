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

        public ArgumentParser(IEnvironment environment, ICurrentBuildAgent buildAgent, IConsole console)
        {
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.console = console ?? throw new ArgumentNullException(nameof(console));
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
                return args;
            }

            var firstArgument = commandLineArguments.First();

            if (firstArgument.IsHelp())
            {
                return new Arguments
                {
                    IsHelp = true,
                };
            }

            if (firstArgument.IsInit())
            {
                return new Arguments
                {
                    TargetPath = System.Environment.CurrentDirectory,
                    Init = true,
                };
            }

            var arguments = new Arguments();

            AddAuthentication(arguments);

            var switchesAndValues = CollectSwitchesAndValuesFromArguments(commandLineArguments, out var firstArgumentIsSwitch);

            for (var i = 0; i < switchesAndValues.AllKeys.Length; i++)
            {
                ParseArguments(arguments, switchesAndValues, i);
            }

            if (arguments.Output.Count == 0)
            {
                arguments.Output.Add(OutputType.Json);
            }

            // If the first argument is a switch, it should already have been consumed in the above loop,
            // or else a WarningException should have been thrown and we wouldn't end up here.
            arguments.TargetPath ??= firstArgumentIsSwitch
                ? System.Environment.CurrentDirectory
                : firstArgument;

            arguments.NoFetch = arguments.NoFetch || buildAgent != null && buildAgent.PreventFetch();

            return arguments;
        }

        private void ParseArguments(Arguments arguments, NameValueCollection switchesAndValues, int i)
        {
            var name = switchesAndValues.AllKeys[i];
            var values = switchesAndValues.GetValues(name);
            var value = values?.FirstOrDefault();

            if (name.IsSwitch("version"))
            {
                EnsureArgumentValueCount(values);
                arguments.IsVersion = true;
                return;
            }

            if (name.IsSwitch("l"))
            {
                EnsureArgumentValueCount(values);
                arguments.LogFilePath = value;
                return;
            }

            if (name.IsSwitch("config"))
            {
                EnsureArgumentValueCount(values);
                arguments.ConfigFile = value;
                return;
            }

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

            if (name.IsSwitch("dynamicRepoLocation"))
            {
                EnsureArgumentValueCount(values);
                arguments.DynamicRepositoryClonePath = value;
                return;
            }

            if (name.IsSwitch("url"))
            {
                EnsureArgumentValueCount(values);
                arguments.TargetUrl = value;
                return;
            }

            if (name.IsSwitch("b"))
            {
                EnsureArgumentValueCount(values);
                arguments.TargetBranch = value;
                return;
            }

            if (name.IsSwitch("u"))
            {
                EnsureArgumentValueCount(values);
                arguments.Authentication.Username = value;
                return;
            }

            if (name.IsSwitch("p"))
            {
                EnsureArgumentValueCount(values);
                arguments.Authentication.Password = value;
                return;
            }

            if (name.IsSwitch("c"))
            {
                EnsureArgumentValueCount(values);
                arguments.CommitId = value;
                return;
            }

            if (name.IsSwitch("exec"))
            {
                EnsureArgumentValueCount(values);
#pragma warning disable CS0612 // Type or member is obsolete
                arguments.Exec = value;
#pragma warning restore CS0612 // Type or member is obsolete
                return;
            }

            if (name.IsSwitch("execargs"))
            {
                EnsureArgumentValueCount(values);
#pragma warning disable CS0612 // Type or member is obsolete
                arguments.ExecArgs = value;
#pragma warning restore CS0612 // Type or member is obsolete
                return;
            }

            if (name.IsSwitch("proj"))
            {
                EnsureArgumentValueCount(values);
#pragma warning disable CS0612 // Type or member is obsolete
                arguments.Proj = value;
#pragma warning restore CS0612 // Type or member is obsolete
                return;
            }

            if (name.IsSwitch("projargs"))
            {
                EnsureArgumentValueCount(values);
#pragma warning disable CS0612 // Type or member is obsolete
                arguments.ProjArgs = value;
#pragma warning restore CS0612 // Type or member is obsolete
                return;
            }

            if (name.IsSwitch("diag"))
            {
                if (value == null || value.IsTrue())
                {
                    arguments.Diag = true;
                }

                return;
            }

            if (name.IsSwitch("updateAssemblyInfo"))
            {
                ParseUpdateAssemblyInfo(arguments, value, values);
                return;
            }

            if (name.IsSwitch("assemblyversionformat"))
            {
                throw new WarningException("assemblyversionformat switch removed, use AssemblyVersioningScheme configuration value instead");
            }

            if (name.IsSwitch("v") || name.IsSwitch("showvariable"))
            {
                ParseShowVariable(value, name, arguments);
                return;
            }

            if (name.IsSwitch("showConfig"))
            {
                ParseShowConfig(value, arguments);

                return;
            }

            if (name.IsSwitch("output"))
            {
                ParseOutput(values, arguments);
                return;
            }

            if (name.IsSwitch("nofetch"))
            {
                arguments.NoFetch = true;
                return;
            }

            if (name.IsSwitch("nonormalize"))
            {
                arguments.NoNormalize = true;
                return;
            }

            if (name.IsSwitch("ensureassemblyinfo"))
            {
                ParseEnsureAssemblyInfo(arguments, value);
                return;
            }

            if (name.IsSwitch("overrideconfig"))
            {
                ParseOverrideConfig(arguments, value);
                return;
            }

            if (name.IsSwitch("nocache"))
            {
                arguments.NoCache = true;
                return;
            }

            if (name.IsSwitch("verbosity"))
            {
                ParseVerbosity(value, arguments);
                return;
            }

            if (name.IsSwitch("updatewixversionfile"))
            {
                arguments.UpdateWixVersionFile = true;
                return;
            }

            var couldNotParseMessage = $"Could not parse command line parameter '{name}'.";

            // If we've reached through all argument switches without a match, we can relatively safely assume that the first argument isn't a switch, but the target path.
            if (i == 0)
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

        private static void ParseShowConfig(string value, Arguments arguments)
        {
            if (value.IsTrue())
            {
                arguments.ShowConfig = true;
            }
            else if (value.IsFalse())
            {
                arguments.ShowConfig = false;
            }
            else
            {
                arguments.ShowConfig = true;
            }
        }

        private static void ParseShowVariable(string value, string name, Arguments arguments)
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

            if (arguments.UpdateAssemblyInfoFileName.Count > 1 && arguments.EnsureAssemblyInfo)
            {
                throw new WarningException("Can't specify multiple assembly info files when using /ensureassemblyinfo switch, either use a single assembly info file or do not specify /ensureassemblyinfo and create assembly info files manually");
            }
        }

        private static void ParseOutput(string[] values, Arguments arguments)
        {
            foreach (var v in values)
            {
                if (!Enum.TryParse(v, true, out OutputType outputType))
                {
                    throw new WarningException($"Value '{v}' cannot be parsed as output type, please use 'json' or 'buildserver'");
                }

                arguments.Output.Add(outputType);
            }
        }

        private static void ParseVerbosity(string value, Arguments arguments)
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
                arguments.UpdateAssemblyInfoFileName.Add(value);
            }
            else
            {
                arguments.UpdateAssemblyInfo = true;
            }

            if (arguments.UpdateAssemblyInfoFileName.Count > 1 && arguments.EnsureAssemblyInfo)
            {
                throw new WarningException("Can't specify multiple assembly info files when using -ensureassemblyinfo switch, either use a single assembly info file or do not specify -ensureassemblyinfo and create assembly info files manually");
            }
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
                arguments.Authentication.Username = password;
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
