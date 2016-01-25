namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class ArgumentParser
    {
        public static Arguments ParseArguments(string commandLineArguments)
        {
            return ParseArguments(commandLineArguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList());
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
            if (IsHelp(firstArgument))
            {
                return new Arguments
                {
                    IsHelp = true
                };
            }
            if (IsInit(firstArgument))
            {
                return new Arguments
                {
                    TargetPath = Environment.CurrentDirectory,
                    Init = true
                };
            }

            if (commandLineArguments.Count == 1 && !(commandLineArguments[0].StartsWith("-") || commandLineArguments[0].StartsWith("/")))
            {
                return new Arguments
                {
                    TargetPath = firstArgument
                };
            }

            List<string> namedArguments;
            var arguments = new Arguments();
            if (firstArgument.StartsWith("-") || firstArgument.StartsWith("/"))
            {
                arguments.TargetPath = Environment.CurrentDirectory;
                namedArguments = commandLineArguments;
            }
            else
            {
                arguments.TargetPath = firstArgument;
                namedArguments = commandLineArguments.Skip(1).ToList();
            }

            var args = CollectSwitchesAndValuesFromArguments(namedArguments);

            foreach (var name in args.AllKeys)
            {
                var values = args.GetValues(name);

                string value = null;

                if (values != null)
                {
                    //Currently, no arguments use more than one value, so having multiple values is an input error.
                    //In the future, this exception can be removed to support multiple values for a switch.
                    if (values.Length > 1) throw new WarningException(string.Format("Could not parse command line parameter '{0}'.", values[1]));

                    value = values.FirstOrDefault();
                }

                if (IsSwitch("l", name))
                {
                    arguments.LogFilePath = value;
                    continue;
                }

                if (IsSwitch("targetpath", name))
                {
                    arguments.TargetPath = value;
                    continue;
                }

                if (IsSwitch("dynamicRepoLocation", name))
                {
                    arguments.DynamicRepositoryLocation = value;
                    continue;
                }

                if (IsSwitch("url", name))
                {
                    arguments.TargetUrl = value;
                    continue;
                }

                if (IsSwitch("b", name))
                {
                    arguments.TargetBranch = value;
                    continue;
                }

                if (IsSwitch("u", name))
                {
                    arguments.Authentication.Username = value;
                    continue;
                }

                if (IsSwitch("p", name))
                {
                    arguments.Authentication.Password = value;
                    continue;
                }

                if (IsSwitch("c", name))
                {
                    arguments.CommitId = value;
                    continue;
                }

                if (IsSwitch("exec", name))
                {
                    arguments.Exec = value;
                    continue;
                }

                if (IsSwitch("execargs", name))
                {
                    arguments.ExecArgs = value;
                    continue;
                }

                if (IsSwitch("proj", name))
                {
                    arguments.Proj = value;
                    continue;
                }

                if (IsSwitch("projargs", name))
                {
                    arguments.ProjArgs = value;
                    continue;
                }

                if (IsSwitch("updateAssemblyInfo", name))
                {
                    if (new[] { "1", "true" }.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        arguments.UpdateAssemblyInfo = true;
                    }
                    else if (new[] { "0", "false" }.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        arguments.UpdateAssemblyInfo = false;
                    }
                    else if (!IsSwitchArgument(value))
                    {
                        arguments.UpdateAssemblyInfo = true;
                        arguments.UpdateAssemblyInfoFileName = value;
                    }
                    else
                    {
                        arguments.UpdateAssemblyInfo = true;
                    }
                    continue;
                }

                if (IsSwitch("assemblyversionformat", name))
                {
                    throw new WarningException("assemblyversionformat switch removed, use AssemblyVersioningScheme configuration value instead");
                }

                if (IsSwitch("v", name) || IsSwitch("showvariable", name))
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

                if (IsSwitch("showConfig", name))
                {
                    if (new[] { "1", "true" }.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        arguments.ShowConfig = true;
                    }
                    else if (new[] { "0", "false" }.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        arguments.UpdateAssemblyInfo = false;
                    }
                    else
                    {
                        arguments.ShowConfig = true;
                    }
                    continue;
                }

                if (IsSwitch("output", name))
                {
                    OutputType outputType;
                    if (!Enum.TryParse(value, true, out outputType))
                    {
                        throw new WarningException(string.Format("Value '{0}' cannot be parsed as output type, please use 'json' or 'buildserver'", value));
                    }

                    arguments.Output = outputType;
                    continue;
                }

                if (IsSwitch("nofetch", name))
                {
                    arguments.NoFetch = true;
                    continue;
                }

                throw new WarningException(string.Format("Could not parse command line parameter '{0}'.", name));
            }

            return arguments;
        }

        static NameValueCollection CollectSwitchesAndValuesFromArguments(List<string> namedArguments)
        {
            var args = new NameValueCollection();

            string currentKey = null;
            for (var index = 0; index < namedArguments.Count; index = index + 1)
            {
                var arg = namedArguments[index];
                //If this is a switch, create new name/value entry for it, with a null value.
                if (IsSwitchArgument(arg))
                {
                    currentKey = arg;
                    args.Add(currentKey, null);
                }
                //If this is a value (not a switch)
                else
                {
                    //And if the current switch does not have a value yet, set it's value to this argument.
                    if (string.IsNullOrEmpty(args[currentKey]))
                    {
                        args[currentKey] = arg;
                    }
                    //Otherwise add the value under the same switch.
                    else
                    {
                        args.Add(currentKey, arg);
                    }
                }
            }
            return args;
        }

        static bool IsSwitchArgument(string value)
        {
            return value != null && (value.StartsWith("-") || value.StartsWith("/"))
                && !Regex.Match(value, @"/\w+:").Success; //Exclude msbuild & project parameters in form /blah:, which should be parsed as values, not switch names.
        }

        static bool IsSwitch(string switchName, string value)
        {
            if (value.StartsWith("-"))
            {
                value = value.Remove(0, 1);
            }

            if (value.StartsWith("/"))
            {
                value = value.Remove(0, 1);
            }

            return (string.Equals(switchName, value, StringComparison.OrdinalIgnoreCase));
        }

        static bool IsInit(string singleArgument)
        {
            return singleArgument.Equals("init", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsHelp(string singleArgument)
        {
            return (singleArgument == "?") ||
                IsSwitch("h", singleArgument) ||
                IsSwitch("help", singleArgument) ||
                IsSwitch("?", singleArgument);
        }
    }
}