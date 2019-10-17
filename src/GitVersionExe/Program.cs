using System;
using System.Diagnostics;
using GitVersion.Common;
using GitVersion.Logging;
using Environment = GitVersion.Common.Environment;

namespace GitVersion
{
    class Program
    {
        static void Main()
        {
            var log = new Logging.Log();
            var fileSystem = new FileSystem();
            var environment = new Environment();
            var argumentParser = new ArgumentParser(fileSystem, log);
            var arguments = argumentParser.ParseArguments();
            log.Verbosity = arguments?.Verbosity ?? Verbosity.Normal;

            var app = new GitVersionApplication(fileSystem, environment, log);

            var exitCode = app.Run(arguments);

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }

            System.Environment.Exit(exitCode);
        }
    }
}
