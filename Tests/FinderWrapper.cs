using System.IO;
using System.Linq;
using GitFlowVersion;
using LibGit2Sharp;
using NCrunch.Framework;

public class FinderWrapper
{
    public static SemanticVersion FindVersionForCommit(string sha, string branchName)
    {
        var repository = GetRepository();
        var branch = repository.Branches.First(x => x.Name == branchName);
        var commit = branch.Commits.First(x => x.Sha == sha);

        var finder = new GitFlowVersionFinder
                     {
                         Commit = commit,
                         Repository = repository,
                         Branch = branch
                     };

        return finder.FindVersion();
    }

    static Repository GetRepository()
    {
        var solutionDirectory = GetSolutionDirectory();

        var gitFlowRepoPath = Path.Combine(solutionDirectory, @"..\GitFlow");
        return new Repository(gitFlowRepoPath);
    }

    static string GetSolutionDirectory()
    {
        var solutionPath = NCrunchEnvironment.GetOriginalSolutionPath();
        if (solutionPath == null)
        {
            var currentDirectory = AssemblyLocation.CurrentDirectory();
            return Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\"));
        }
        return Path.GetDirectoryName(solutionPath);
    }
}