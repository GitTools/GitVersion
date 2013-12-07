namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class ArgumentParser
    {
        public static Arguments ParseArguments(string commandLineArguments)
        {
            return ParseArguments(commandLineArguments.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).ToList());
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

            EnsureArgumentsEvenCount(commandLineArguments, namedArguments);

            for (var index = 0; index < namedArguments.Count; index = index+2)
            {
                var name = namedArguments[index];
                var value = namedArguments[index + 1];

                if (IsSwitch("l", name))
                {
                    arguments.LogFilePath = value;
                    continue;
                }

                if ((IsSwitch("v", name)) && VersionParts.Contains(value.ToLower()))
                {
                    arguments.VersionPart = value.ToLower();
                    continue;
                }

                if (IsSwitch("u", name))
                {
                    arguments.TargetUrl = value;
                    continue;
                }

                if (IsSwitch("b", name))
                {
                    arguments.TargetBranch = value;
                    continue;
                }

                throw new ErrorException(string.Format("Could not parse command line parameter '{0}'.", name));
            }
            return arguments;
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

            return (string.Equals(switchName, value));
        }

        static void EnsureArgumentsEvenCount(List<string> commandLineArguments, List<string> namedArguments)
        {
            if (namedArguments.Count.IsOdd())
            {
                var message = string.Format("Could not parse arguments: '{0}'.", string.Join(" ", commandLineArguments));
                throw new ErrorException(message);
            }
        }

        static bool IsHelp(string singleArgument)
        {
            return (singleArgument == "?") ||
                   (singleArgument == "/h") ||
                   (singleArgument == "/help") ||
                   (singleArgument == "/?") ||
                   (singleArgument == "-h") ||
                   (singleArgument == "-help") ||
                   (singleArgument == "-?");
        }

        static string[] VersionParts = {"major", "minor", "patch", "long", "short", "nuget"};
    }
}