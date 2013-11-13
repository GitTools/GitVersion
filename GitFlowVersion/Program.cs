namespace GitFlowVersion
{
    using System;

    public class Program
    {
        private static void Main(string[] args)
        {
            // TODO: What about SignAssembly?
            if (args.Length != 1)
            {
                Console.WriteLine("Please provide the solution directory as input argument.");
            }

            string solutionDirectory = args[0];

            var gitDirectory = GitDirFinder.TreeWalkForGitDir(solutionDirectory);

            if (string.IsNullOrEmpty(gitDirectory))
            {
                // TODO: What do we do here? 
            }

            VersionAndBranch versionAndBranch = VersionCache.GetVersion(gitDirectory);
            var format = string.Format(@"{{ ""Version"": ""{0}"", ""BranchType"": ""{1}"", ""BranchName"": ""{2}"", ""Sha"": ""{3}"" }}", versionAndBranch.ToShortString(), versionAndBranch.BranchType, versionAndBranch.BranchName, versionAndBranch.Sha);
            Console.WriteLine(format);
        }
    }
}