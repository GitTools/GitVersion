using System;
using System.Diagnostics;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;
using Console = System.Console;
using Environment = GitVersion.Common.Environment;

namespace GitVersion
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileSystem = new FileSystem();
            var environment = new Environment();
            var argumentParser = new ArgumentParser();

            int exitCode;
            try
            {
                var arguments = argumentParser.ParseArguments(args);
                var log = new Log
                {
                    Verbosity = arguments.Verbosity
                };

                var configFileLocator = string.IsNullOrWhiteSpace(arguments.ConfigFile)
                    ? (IConfigFileLocator) new DefaultConfigFileLocator(fileSystem, log)
                    : new NamedConfigFileLocator(arguments.ConfigFile, fileSystem, log);

                var app = new GitVersionApplication(fileSystem, environment, log, configFileLocator);

                exitCode = app.Run(arguments);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                exitCode = 1;
            }

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }

            System.Environment.Exit(exitCode);
        }
    }
}
