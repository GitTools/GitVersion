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

                var applicableBuildServers = GetApplicableBuildServers().ToList();

                foreach (var buildServer in applicableBuildServers)
                {
                    buildServer.PerformPreProcessingSteps(gitDirectory);
                }

                var versionAndBranch = VersionCache.GetVersion(gitDirectory);

                var versionAsKeyValue = versionAndBranch.ToKeyValue();
                switch (arguments.VersionPart)
                {
                    case null:
                        Console.WriteLine(JsonOutputFormatter.ToJson(versionAsKeyValue)); 
                        break;
                    default:
                        string part;
                        if (!versionAsKeyValue.TryGetValue(arguments.VersionPart, out part))
                        {
                            throw new ErrorException(string.Format("Could not extract '{0}' from the available parts.", arguments.VersionPart));
                        }
                        Console.WriteLine(part);
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
        }

        static IEnumerable<IBuildServer> GetApplicableBuildServers()
        {
            return BuildServerList.BuildServers.Where(buildServer => buildServer.CanApplyToCurrentContext());
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