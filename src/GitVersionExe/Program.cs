using System;
using System.Diagnostics;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;
using Console = System.Console;
using Environment = GitVersion.Common.Environment;

namespace GitVersion
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var arguments = ParseArguments(args);

            var exitCode = 1;
            if (arguments != null)
            {
                var fileSystem = new FileSystem();
                var environment = new Environment();

                try
                {
                    var log = new Log { Verbosity = arguments.Verbosity };

                    var configFileLocator = string.IsNullOrWhiteSpace(arguments.ConfigFile)
                        ? (IConfigFileLocator) new DefaultConfigFileLocator(fileSystem, log)
                        : new NamedConfigFileLocator(arguments.ConfigFile, fileSystem, log);

                    var app = new GitVersionApplication(fileSystem, environment, log, configFileLocator);

                    exitCode = app.Run(arguments);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(exception.Message);
                }
            }

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }

            System.Environment.Exit(exitCode);
        }

        private static Arguments ParseArguments(string[] args)
        {
            var argumentParser = new ArgumentParser();
            Arguments arguments = null;
            try
            {
                arguments = argumentParser.ParseArguments(args);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
            return arguments;
        }
    }
}
