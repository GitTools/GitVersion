using LibGit2Sharp;
using System;
using System.IO;

namespace GitVersion.Cli
{
    public class GitRepositoryDirectoryInfo
    {
        private readonly Func<string> getRepoWorkingDirectory;
        private Lazy<string> workingDirectory;

        public GitRepositoryDirectoryInfo(string dotGitDirectory, Func<string> getRepoWorkingDirectory)
        {
            DotGitDirectory = dotGitDirectory;
            this.getRepoWorkingDirectory = getRepoWorkingDirectory;
            workingDirectory = new Lazy<string>(() =>
            {
                return getRepoWorkingDirectory();               
            });
        }

        public string DotGitDirectory { get; }

        public string WorkingDirectory { get { return workingDirectory.Value; } }

        public static GitRepositoryDirectoryInfo Get(string dotGitDirectory, string environmentWorkingDirectory)
        {
            if (string.IsNullOrEmpty(dotGitDirectory))
            {
                dotGitDirectory = Repository.Discover(environmentWorkingDirectory);
            }

            dotGitDirectory = dotGitDirectory?.TrimEnd('/', '\\');

            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException($"Can't find the .git directory in {dotGitDirectory}");

            dotGitDirectory = dotGitDirectory.Contains(Path.Combine(".git", "worktrees"))
                ? Directory.GetParent(Directory.GetParent(dotGitDirectory).FullName).FullName
                : dotGitDirectory;

            using var repository = new Repository(dotGitDirectory);
            return new GitRepositoryDirectoryInfo(dotGitDirectory, () => {
                using var repository = new Repository(dotGitDirectory);
                return repository.Info.WorkingDirectory;
            });          
        }

    }


}
