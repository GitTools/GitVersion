namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text.RegularExpressions;
    using NDesk.Options;


    public class ArgumentParser
    {
        public static OptionSet GetOptionSet(Arguments arguments)
        {
            return new CaseInsensitiveOptionSet
                {
                    {
                        "h|?|help", "Show this message and exit",
                        v => arguments.IsHelp = (v != null)
                    },
                    {
                        "l|log=", "Path to logfile",
                        v =>
                            {
                                arguments.LogFilePath = v;
                                arguments.TargetPath = Environment.CurrentDirectory;
                            }
                    },
                    {
                        "exec=", "Executes target executable making GitVersion variables available as environmental variables",
                        v => arguments.Exec = v
                    },
                    {
                        "execargs=", "Arguments for the executable specified by /exec",
                        v => arguments.ExecArgs = v
                    },
                    {
                        "proj=", "Build an msbuild file, GitVersion variables will be passed as msbuild properties",
                        v => arguments.Proj = v
                    },
                    {
                        "projargs=", "Additional arguments to pass to msbuild",
                        v => arguments.ProjArgs = v
                    },
                    {
                        "u|user=", "Username in case authentication is required",
                        v => arguments.Authentication.Username = v
                    },
                    {
                        "p|password=", "Password in case authentication is required",
                        v => arguments.Authentication.Password = v
                    },
                    {
                        "output=", "Determines the output to the console. Can be either 'json' or 'buildserver', will default to 'json'",
                        v => arguments.SetOutPutType(v)
                    },
                    {
                        "url=", "Url to remote git repository",
                        v => arguments.TargetUrl = v
                    },
                    {
                        "b=", "Name of the branch to use on the remote repository, must be used in combination with /url",
                        v => arguments.TargetBranch = v
                    },
                    {
                        "updateassemblyinfo", "Will recursively search for all 'AssemblyInfo.cs' files in the git repo and update them",
                        v => arguments.UpdateAssemblyInfo = (v != null)
                    },
                    {
                        "dynamicrepolocation=", "By default dynamic repositories will be cloned to %tmp%. Use this switch to override",
                        v => arguments.DynamicRepositoryLocation = v
                    },
                    {
                        "nofetch", "", // help text missing
                        v => arguments.NoFetch = (v != null)
                    },
                };
        }

        public static Arguments ParseArguments(string commandLineArguments)
        {
            return ParseArguments(commandLineArguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList());
        }

        public static Arguments ParseArguments(List<string> commandLineArguments)
        {
            if (commandLineArguments.Count <= 0)
            {
                return new Arguments()
                    {
                        TargetPath = Environment.CurrentDirectory
                    };
            }

            var arguments = new Arguments();

            var p = GetOptionSet(arguments);
            var additionalArguments = p.Parse(commandLineArguments);

            ParseSpecialArguments(additionalArguments, arguments);

            return arguments;

/*
            // Following code is not or implicitly tested:

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

                if (IsSwitch("targetpath", name))
                {
                    arguments.TargetPath = value;
                    continue;
                }

                if (IsSwitch("c", name))
                {
                    arguments.CommitId = value;
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
                        var message = string.Format(messageFormat, name, String.Join(", ", VersionVariables.AvailableVariables.Select(x=>string.Concat("'", x, "'"))));
                        throw new WarningException(message);
                    }

                    arguments.ShowVariable = versionVariable;
                    continue;
                }

                if (IsSwitch("showConfig", name))
                {
                    if (new[] { "1", "true" }.Contains(value))
                    {
                        arguments.ShowConfig = true;
                    }
                    else if (new[] { "0", "false" }.Contains(value))
                    {
                        arguments.UpdateAssemblyInfo = false;
                    }
                    else
                    {
                        arguments.ShowConfig = true;                        
                    }
                    continue;
                }

                throw new WarningException(string.Format("Could not parse command line parameter '{0}'.", name));
            }

            return arguments;
*/
        }

        static void ParseSpecialArguments(List<string> additionalArguments, Arguments arguments)
        {
            if (additionalArguments.Count <= 0)
            {
                return;
            }

            var firstArgument = additionalArguments[0];

            if (IsInit(firstArgument))
            {
                arguments.TargetPath = Environment.CurrentDirectory;
                arguments.Init = true; // should be replaced by --init switch
            }
            else if (IsSwitchArgument(firstArgument))
            {
                throw new WarningException(string.Format("Could not parse command line parameter '{0}'.", firstArgument));
            }
            else
            {
                // TODO: should not overwrite if --targetPath is specified
                arguments.TargetPath = firstArgument;
            }

            if (additionalArguments.Count > 1)
            {
                // fail on first unknown argument:
                throw new WarningException(string.Format("Could not parse command line parameter '{0}'.", additionalArguments[1]));
            }
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
                    if (String.IsNullOrEmpty(args[currentKey]))
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
    }
}