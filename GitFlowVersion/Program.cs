namespace GitFlowVersion
{
    using System;

    public class Program
    {
        static void Main(string[] args)
        {
            string solutionDirectory;
            if (args.Length == 1)
            {
                solutionDirectory = args[0];
            }
            else
            {
                solutionDirectory = Environment.CurrentDirectory;
            }


            var gitDirectory = GitDirFinder.TreeWalkForGitDir(solutionDirectory);

            if (string.IsNullOrEmpty(gitDirectory))
            {
                Console.Error.WriteLine("Could not find .git directory");
                Environment.Exit(1);
            }

            var json = VersionCache.GetVersion(gitDirectory).ToJson();

            Console.WriteLine(json);
        }
    }
}