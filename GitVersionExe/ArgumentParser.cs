namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

 
    public class ArgumentParser
    {
        static ArgumentParser()
        {
            var fields = typeof(VariableProvider).GetFields(BindingFlags.Public | BindingFlags.Static);
            VersionParts = fields.Select(x => x.Name.ToLower()).ToArray();
        }

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

            if (commandLineArguments.Count == 1)
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

            for (var index = 0; index < namedArguments.Count; index = index + 2)
            {
                var name = namedArguments[index];
                var value = namedArguments.Count > index + 1 ? namedArguments[index + 1] : null;

                if (IsSwitch("l", name))
                {
                    arguments.LogFilePath = value;
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
                    if (new[] { "1", "true" }.Contains(value))
                    {
                        arguments.UpdateAssemblyInfo = true;
                    }
                    else if (new[] { "0", "false" }.Contains(value))
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
                        index--;
                    }
                    continue;
                }

                if (IsSwitch("assemblyversionformat", name))
                {
                    arguments.AssemblyVersionFormat = value;
                    continue;
                }

                if ((IsSwitch("v", name)) && VersionParts.Contains(value.ToLower()))
                {
                    arguments.VersionPart = value.ToLower();
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

                throw new WarningException(string.Format("Could not parse command line parameter '{0}'.", name));
            }
            return arguments;
        }

        static bool IsSwitchArgument(string value)
        {
            return value != null && value.StartsWith("-") || value.StartsWith("/");
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

            return (string.Equals(switchName, value, StringComparison.InvariantCultureIgnoreCase));
        }

        static bool IsInit(string singleArgument)
        {
            return singleArgument.Equals("init", StringComparison.InvariantCultureIgnoreCase);
        }

        static bool IsHelp(string singleArgument)
        {
            return (singleArgument == "?") ||
                IsSwitch("h", singleArgument) ||
                IsSwitch("help", singleArgument) ||
                IsSwitch("?", singleArgument);
        }

        static string[] VersionParts;
    }
}