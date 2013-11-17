namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class Program
    {
        static void Main()
        {
            try
            {
                var arguments = ArgumentParser.ParseArguments(GetArgumentsWithoutExeName());

                if (arguments.IsHelp)
                {
                    HelpWriter.Write();
                }
                Logger.WriteInfo = s => File.WriteAllText(arguments.LogFilePath,arguments.LogFilePath);
                var gitDirectory = GitDirFinder.TreeWalkForGitDir(arguments.TargetDirectory);

                if (string.IsNullOrEmpty(gitDirectory))
                {
                    Console.Error.WriteLine("Could not find .git directory");
                    Environment.Exit(1);
                }

                var json = VersionCache.GetVersion(gitDirectory).ToJson();

                Console.WriteLine(json);
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
        }

        static List<string> GetArgumentsWithoutExeName()
        {
            return Environment.GetCommandLineArgs()
                .Skip(1)
                .ToList();
        }
    }
}