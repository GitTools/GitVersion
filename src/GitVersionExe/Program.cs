namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using CommandLine;
    using GitVersion.Options;

    class Program
    {
        static StringBuilder log = new StringBuilder();

        static void Main()
        {
            var exitCode = VerifyArgumentsAndRun();

            if (exitCode != 0)
            {
                Console.Write(log.ToString());
            }

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }

            Environment.Exit(exitCode);
        }

        static int VerifyArgumentsAndRun()
        {
            var args = GetArgumentsWithoutExeName();
            try
            { 
                Parser.Default.ParseArguments<InspectOptions,
                InitOptions,
                InspectRemoteRepositoryOptions,
                InjectBuildServerOptions,
                InjectMsBuildOptions,
                InjectProcess,
                InjectAssemblyInfo>(args)
                .WithParsed<InspectOptions>(InspectRunner.Run)
                .WithParsed<InitOptions>(InitRunner.Run)
                //.WithParsed<InspectRemoteRepositoryOptions>(Runner.Run)
                .WithParsed<InjectBuildServerOptions>(InjectBuildServerRunner.Run)
                //.WithParsed<InjectMsBuildOptions>(Runner.Run)
                //.WithParsed<InjectProcess>(Runner.Run)
                //.WithParsed<InjectAssemblyInfo>(Runner.Run)
                .WithNotParsed(HandleParseErrors);

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }
        }

        static void HandleParseErrors(IEnumerable<Error> commandLineErrors)
        {
            var message = commandLineErrors.Aggregate("Error parsing arguments ...", 
                (current, err) => current + ("\nFailed to parse - " + err));
            throw new WarningException(message);
        }

        static string[] GetArgumentsWithoutExeName()
        {
            return Environment.GetCommandLineArgs()
                              .Skip(1)
                              .ToArray();
        }


        // Logging stuf

        static void ConfigureLogging(Arguments arguments)
        {
            var writeActions = new List<Action<string>>
                {
                    s => log.AppendLine(s)
                };

            if (arguments.Output == OutputType.BuildServer || arguments.LogFilePath == "console" || arguments.Init)
            {
                writeActions.Add(Console.WriteLine);
            }

            if (arguments.LogFilePath != null && arguments.LogFilePath != "console")
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(arguments.LogFilePath));
                    if (File.Exists(arguments.LogFilePath))
                    {
                        using (File.CreateText(arguments.LogFilePath))
                        {
                        }
                    }

                    writeActions.Add(x => WriteLogEntry(arguments, x));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to configure logging: " + ex.Message);
                }
            }

            Logger.SetLoggers(
                s => writeActions.ForEach(a => a(s)),
                s => writeActions.ForEach(a => a(s)),
                s => writeActions.ForEach(a => a(s)));
        }

        static void WriteLogEntry(Arguments arguments, string s)
        {
            var contents = string.Format("{0}\t\t{1}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), s);
            File.AppendAllText(arguments.LogFilePath, contents);
        }
    }
}