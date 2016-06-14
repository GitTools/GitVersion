namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;

    public class ArgumentParser
    {
        public static Arguments ParseArguments(string commandLineArguments)
        {
            var arguments = commandLineArguments
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            return ParseArguments(arguments);
        }

        static void EnsureArgumentValueCount(string[] values, int maxArguments = 1)
        {
            if (values != null && values.Length > maxArguments)
            {
                throw new WarningException(string.Format("Could not parse command line parameter '{0}'.", values[1]));
            }
        }

        public static Arguments ParseArguments(List<string> commandLineArguments)
        {
            if (commandLineArguments.Count == 0)
            {
                return new Arguments
                {
                    TargetPath = Environment.CurrentDirectory
                };
            }

            var firstArgument = commandLineArguments.First();

            if (firstArgument.IsHelp())
            {
                return new Arguments
                {
                    IsHelp = true
                };
            }

            if (firstArgument.IsInit())
            {
                return new Arguments
                {
                    TargetPath = Environment.CurrentDirectory,
                    Init = true
                };
            }

            var arguments = new Arguments();
            bool firstArgumentIsSwitch;
            var switchesAndValues = CollectSwitchesAndValuesFromArguments(commandLineArguments, out firstArgumentIsSwitch);

            for (var i = 0; i < switchesAndValues.AllKeys.Length; i++)
            {
                var name = switchesAndValues.AllKeys[i];
                var values = switchesAndValues.GetValues(name);
                var value = values != null ? values.FirstOrDefault() : null;

                if (name.IsSwitch("l"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.LogFilePath = value;
                    continue;
                }

                if (name.IsSwitch("targetpath"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.TargetPath = value;
                    continue;
                }

                if (name.IsSwitch("dynamicRepoLocation"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.DynamicRepositoryLocation = value;
                    continue;
                }

                if (name.IsSwitch("url"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.TargetUrl = value;
                    continue;
                }

                if (name.IsSwitch("b"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.TargetBranch = value;
                    continue;
                }

                if (name.IsSwitch("u"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.Authentication.Username = value;
                    continue;
                }

                if (name.IsSwitch("p"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.Authentication.Password = value;
                    continue;
                }

                if (name.IsSwitch("c"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.CommitId = value;
                    continue;
                }

                if (name.IsSwitch("exec"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.Exec = value;
                    continue;
                }

                if (name.IsSwitch("execargs"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.ExecArgs = value;
                    continue;
                }

                if (name.IsSwitch("proj"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.Proj = value;
                    continue;
                }

                if (name.IsSwitch("projargs"))
                {
                    EnsureArgumentValueCount(values);
                    arguments.ProjArgs = value;
                    continue;
                }


                if (name.IsSwitch("updateAssemblyInfo"))
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
                            arguments.AddAssemblyInfoFileName(v);
                        }
                    }
                    else if (!value.IsSwitchArgument())
                    {
                        arguments.UpdateAssemblyInfo = true;
                        arguments.AddAssemblyInfoFileName(value);
                    }
                    else
                    {
                        arguments.UpdateAssemblyInfo = true;
                    }

                    if (arguments.UpdateAssemblyInfoFileName.Count > 1 && arguments.EnsureAssemblyInfo)
                    {
                        throw new WarningException("Can't specify multiple assembly info files when using -ensureassemblyinfo switch, either use a single assembly info file or do not specify -ensureassemblyinfo and create assembly info files manually");
                    }

                    continue;
                }

                if (name.IsSwitch("assemblyversionformat"))
                {
                    throw new WarningException("assemblyversionformat switch removed, use AssemblyVersioningScheme configuration value instead");
                }

                if (name.IsSwitch("v") || name.IsSwitch("showvariable"))
                {
                    string versionVariable = null;

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        versionVariable = VersionVariables.AvailableVariables.SingleOrDefault(av => av.Equals(value.Replace("'", ""), StringComparison.CurrentCultureIgnoreCase));
                    }

                    if (versionVariable == null)
                    {
                        var messageFormat = "{0} requires a valid version variable.  Available variables are:\n{1}";
                        var message = string.Format(messageFormat, name, String.Join(", ", VersionVariables.AvailableVariables.Select(x => string.Concat("'", x, "'"))));
                        throw new WarningException(message);
                    }

                    arguments.ShowVariable = versionVariable;
                    continue;
                }

                if (name.IsSwitch("showConfig"))
                {
                    if (value.IsTrue())
                    {
                        arguments.ShowConfig = true;
                    }
                    else if (value.IsFalse())
                    {
                        arguments.UpdateAssemblyInfo = false;
                    }
                    else
                    {
                        arguments.ShowConfig = true;
                    }
                    continue;
                }

                if (name.IsSwitch("output"))
                {
                    OutputType outputType;
                    if (!Enum.TryParse(value, true, out outputType))
                    {
                        throw new WarningException(string.Format("Value '{0}' cannot be parsed as output type, please use 'json' or 'buildserver'", value));
                    }

                    arguments.Output = outputType;
                    continue;
                }

                if (name.IsSwitch("nofetch"))
                {
                    arguments.NoFetch = true;
                    continue;
                }

                if (name.IsSwitch("ensureassemblyinfo"))
                {
                    if (value.IsTrue())
                    {
                        arguments.EnsureAssemblyInfo = true;
                    }
                    else if (value.IsFalse())
                    {
                        arguments.EnsureAssemblyInfo = false;
                    }
                    else
                    {
                        arguments.EnsureAssemblyInfo = true;
                    }

                    if (arguments.UpdateAssemblyInfoFileName.Count > 1 && arguments.EnsureAssemblyInfo)
                    {
                        throw new WarningException("Can't specify multiple assembly info files when using /ensureassemblyinfo switch, either use a single assembly info file or do not specify /ensureassemblyinfo and create assembly info files manually");
                    }
                    continue;
                }

                if (name.IsSwitch("overrideconfig"))
                {
                    var keyValueOptions = (value ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (keyValueOptions.Length == 0)
                    {
                        continue;
                    }

                    arguments.HasOverrideConfig = true;

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
                            throw new WarningException(string.Format("Could not parse /overrideconfig option: {0}. Ensure it is in format 'key=value'", keyValueOption));
                        }

                        var optionKey = keyAndValue[0].ToLowerInvariant();
                        switch (optionKey)
                        {
                            case "tag-prefix":
                                arguments.OverrideConfig.TagPrefix = keyAndValue[1];
                                break;
                            default:
                                throw new WarningException(string.Format("Could not parse /overrideconfig option: {0}. Currently supported only 'tag-prefix' option", optionKey));
                        }
                    }

                    continue;
                }

                var couldNotParseMessage = string.Format("Could not parse command line parameter '{0}'.", name);

                // If we've reached through all argument switches without a match, we can relatively safely assume that the first argument isn't a switch, but the target path.
                if (i == 0)
                {
                    if (name.StartsWith("/"))
                    {
                        if (Path.DirectorySeparatorChar == '/' && name.IsValidPath())
                        {
                            arguments.TargetPath = name;
                            continue;
                        }
                    }
                    else if (!name.IsSwitchArgument())
                    {
                        arguments.TargetPath = name;
                        continue;
                    }

                    couldNotParseMessage += " If it is the target path, make sure it exists.";
                }

                throw new WarningException(couldNotParseMessage);
            }

            if (arguments.TargetPath == null)
            {
                // If the first argument is a switch, it should already have been consumed in the above loop,
                // or else a WarningException should have been thrown and we wouldn't end up here.
                arguments.TargetPath = firstArgumentIsSwitch
                    ? Environment.CurrentDirectory
                    : firstArgument;
            }

            return arguments;
        }

        static NameValueCollection CollectSwitchesAndValuesFromArguments(IList<string> namedArguments, out bool firstArgumentIsSwitch)
        {
            firstArgumentIsSwitch = true;
            var switchesAndValues = new NameValueCollection();
            string currentKey = null;
            var argumentRequiresValue = false;

            for (var i = 0; i < namedArguments.Count; i = i + 1)
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