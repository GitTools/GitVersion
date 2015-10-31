namespace GitVersion
{
    using System;
    using System.Collections.Generic;
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
                        "path|targetpath=", 
                        "The path to inspect\nDefaults to the current directory.",
                        v => arguments.TargetPath = v
                    },
                    {
                        "i|init", "Start the configuration utility for gitversion",
                        v => { throw new WarningException("assemblyversionformat switch removed, use AssemblyVersioningScheme configuration value instead"); }
                    }, 
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
                        "execargs=", "Arguments for the executable specified by --exec",
                        v => arguments.ExecArgs = v
                    },
                    {
                        "proj=", "Build an msbuild file making GitVersion variables available as msbuild properties",
                        v => arguments.Proj = v
                    },
                    {
                        "projargs=", "Additional arguments to pass to msbuild",
                        v => arguments.ProjArgs = v
                    },
                    {
                        "u|username=", "Username in case authentication is required",
                        v => arguments.Authentication.Username = v
                    },
                    {
                        "p|password=", "Password in case authentication is required",
                        v => arguments.Authentication.Password = v
                    },
                    {
                        "o|output=", "Determines the output to the console\nCan be either 'json' or 'buildserver', will default to 'json'.",
                        v => arguments.SetOutPutType(v)
                    },
                    {
                        "url=", "Url to remote git repository",
                        v => arguments.TargetUrl = v
                    },
                    {
                        "b|remotebranch=", "Name of the branch to use on the remote repository, must be used in combination with --url",
                        v => arguments.TargetBranch = v
                    },
                    {
                        "updateassemblyinfo:", "* Will recursively search for all 'AssemblyInfo.cs' files in the git repo and update them\n" +
                        "To use another filename use --updateassemblyinfo:[another-assemblyinfo-filename.cs]",
                        v => 
                        {
                            if (v == null)
                            {
                                arguments.UpdateAssemblyInfo = true;
                            }
                            else
                            {
                                arguments.UpdateAssemblyInfo = true;
                                arguments.UpdateAssemblyInfoFileName = v;
                            }
                        }
                    },
                    {
                        "updateassemblyinfoname", "* Deprecated: use --updateassemblyinfo:[assemblyinfofilename.cs] instead.",
                        v => { throw new WarningException("updateassemblyinfoname deprecated, use --updateassemblyinfo=[assemblyinfo.cs] instead"); }
                    },
                    {
                        "dynamicrepolocation=", "Override locations dynamic repositories are clonden to\nDefaults to %tmp%.",
                        v => arguments.DynamicRepositoryLocation = v
                    },                  
                    {
                        "c|commit=", "The commit id to inspect\nDefaults to the latest available commit on the specified branch.",
                        v => arguments.CommitId = v
                    },                   
                    {
                        "v|showvariable=", "Used in conjuntion with /output json, will output just a particular variable",
                        v => arguments.SetShowVariable(v)
                    },
                    {
                        "nofetch", "",
                        v => arguments.NoFetch = (v != null)
                    },
                    {
                        "showconfig", "Outputs the effective GitVersion config\nOutputs the defaults and custom from GitVersion.yaml in yaml format.",
                        v => arguments.ShowConfig = (v != null)
                    },
                    {
                        "assemblyversionformat", "Deprecated: use AssemblyVersioningScheme configuration value instead.",
                        v => { throw new WarningException("assemblyversionformat switch removed, use AssemblyVersioningScheme configuration value instead"); }
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
        }

        static void ParseSpecialArguments(List<string> additionalArguments, Arguments arguments)
        {
            // if the first argument is "init" or filename, they get special treatment
            // it is probably best to deprecate these and replace:
            // "init" -> -i --init flag
            // "path" -> --path --targetpath option (already exists, add aliases)
            // but for now, these special cases are kept as-is for backwards compatibility

            if (additionalArguments.Count <= 0)
            {
                return;
            }

            var firstArgument = additionalArguments[0];

            if (IsInit(firstArgument))
            {
                arguments.TargetPath = Environment.CurrentDirectory;
                arguments.Init = true;
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


        static bool IsSwitchArgument(string value)
        {
            return value != null && (value.StartsWith("-") || value.StartsWith("/")) 
                && !Regex.Match(value, @"/\w+:").Success; //Exclude msbuild & project parameters in form /blah:, which should be parsed as values, not switch names.
        }

        static bool IsInit(string singleArgument)
        {
            return singleArgument.Equals("init", StringComparison.InvariantCultureIgnoreCase);
        }

    }
}