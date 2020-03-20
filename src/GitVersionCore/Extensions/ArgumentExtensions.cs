using System.IO;
using LibGit2Sharp;

namespace GitVersion.Extensions
{
    public static class ArgumentExtensions
    {
        public static string GetWorkingDirectory(this Arguments arguments) => arguments.TargetPath?.TrimEnd('/', '\\') ?? string.Empty;

        public static bool IsDynamicGitRepository(this Arguments arguments) => !string.IsNullOrWhiteSpace(arguments.DynamicGitRepositoryPath);

        public static string GetDotGitDirectory(this Arguments arguments)
        {
            var gitDirectory = arguments.IsDynamicGitRepository() ? arguments.DynamicGitRepositoryPath : Repository.Discover(arguments.WorkingDirectory);

            gitDirectory = gitDirectory?.TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(gitDirectory))
                throw new DirectoryNotFoundException("Can't find the .git directory in " + gitDirectory);

            return gitDirectory.Contains(Path.Combine(".git", "worktrees"))
                ? Directory.GetParent(Directory.GetParent(gitDirectory).FullName).FullName
                : gitDirectory;
        }

        public static string GetProjectRootDirectory(this Arguments arguments)
        {
            if (arguments.IsDynamicGitRepository())
            {
                return arguments.WorkingDirectory;
            }

            var dotGitDirectory = Repository.Discover(arguments.WorkingDirectory);

            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException($"Can't find the .git directory in {dotGitDirectory}");

            using var repo = new Repository(dotGitDirectory);
            return repo.Info.WorkingDirectory;
        }
    }
}
