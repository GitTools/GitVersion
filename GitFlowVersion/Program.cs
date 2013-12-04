namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    class Program
    {
        static void Main()
        {
            try
            {
                var arguments = ArgumentParser.ParseArguments(GetArgumentsWithoutExeName());

                GitFlowVersionEnvironment.Initialize(arguments);

                if (arguments.IsHelp)
                {
                    HelpWriter.Write();
                    return;
                }

                ConfigureLogging(arguments);

                var gitPreparer = new GitPreparer(arguments.TargetPath, arguments.TargetUrl, arguments.TargetBranch);
                var gitDirectory = gitPreparer.Prepare();
                if (string.IsNullOrWhiteSpace(gitDirectory))
                {
                    Console.Error.WriteLine("Failed to prepare or find the .git directory in path '{0}'", arguments.TargetPath);
                    Environment.Exit(1);
                }

                var json = VersionCache.GetVersion(gitDirectory).ToJson();

                Console.WriteLine(json);

                if (gitPreparer.IsDynamicGitRepository)
                {
                    DeleteHelper.DeleteGitRepository(gitPreparer.DynamicGitRepositoryPath);
                }
            }
            catch (ErrorException exception)
            {
                Console.Error.Write("An error occurred:\r\n{0}", exception.Message);
                Environment.Exit(1);
            }
            catch (Exception exception)
            {
                Console.Error.Write("An unexpected error occurred:\r\n{0}", exception);
                Environment.Exit(1);
            }
#if DEBUG
            if (Debugger.IsAttached)
            {
                Console.ReadLine();
            }
#endif
        }

        static void ConfigureLogging(Arguments arguments)
        {
            if (arguments.LogFilePath == null)
            {
                Logger.WriteInfo = s =>{};
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(arguments.LogFilePath));
                if (File.Exists(arguments.LogFilePath))
                {
                    using (File.CreateText(arguments.LogFilePath)) { }   
                }

                Logger.WriteInfo = s => WriteLogEntry(arguments, s);
            }
        }

        static void WriteLogEntry(Arguments arguments, string s)
        {
            var contents = string.Format("{0}\t\t{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), s);
            File.WriteAllText(arguments.LogFilePath, contents);
        }

        static List<string> GetArgumentsWithoutExeName()
        {
            return Environment.GetCommandLineArgs()
                .Skip(1)
                .ToList();
        }
    }
}