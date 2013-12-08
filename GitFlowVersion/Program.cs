namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    class Program
    {
        static void Main()
        {
            try
            {
                var arguments = ArgumentParser.ParseArguments(GetArgumentsWithoutExeName());

                if (arguments.IsHelp)
                {
                    HelpWriter.Write();
                    return;
                }
                ConfigureLogging(arguments);
                var gitDirectory = GitDirFinder.TreeWalkForGitDir(arguments.TargetPath);

                if (string.IsNullOrEmpty(gitDirectory))
                {
                    Console.Error.WriteLine("Could not find .git directory");
                    Environment.Exit(1);
                }

                var versionAndBranch = VersionCache.GetVersion(gitDirectory);

                switch (arguments.VersionPart)
                {
                    case null: 
                        Console.WriteLine(versionAndBranch.ToJson()); 
                        break;
                    case "nuget":
                        Console.WriteLine(versionAndBranch.GenerateNugetVersion()); 
                        break;
                    case "major":
                        Console.WriteLine(versionAndBranch.Version.Major);
                        break;
                    case "minor":
                        Console.WriteLine(versionAndBranch.Version.Minor);
                        break;
                    case "patch":
                        Console.WriteLine(versionAndBranch.Version.Patch);
                        break;
                    case "long":
                        Console.WriteLine(versionAndBranch.ToLongString());
                        break;
                    case "short":
                        Console.WriteLine(versionAndBranch.ToShortString());
                        break;
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