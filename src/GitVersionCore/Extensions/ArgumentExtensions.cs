using System.IO;
using LibGit2Sharp;

namespace GitVersion.Extensions
{
    public static class ArgumentExtensions
    {
        public static string GetTargetUrl(this Arguments arguments) => arguments.TargetUrl;

        public static string GetWorkingDirectory(this Arguments arguments) => arguments.TargetPath.TrimEnd('/', '\\');

        public static bool IsDynamicGitRepository(this Arguments arguments) => !string.IsNullOrWhiteSpace(arguments.DynamicGitRepositoryPath);

        public static string GetDotGitDirectory(this Arguments arguments)
        {
            var gitDirectory = arguments.IsDynamicGitRepository() ? arguments.DynamicGitRepositoryPath : Repository.Discover(arguments.GetWorkingDirectory());

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
                return arguments.GetWorkingDirectory();
            }

            var dotGitDirectory = Repository.Discover(arguments.GetWorkingDirectory());

            if (string.IsNullOrEmpty(dotGitDirectory))
                throw new DirectoryNotFoundException($"Can't find the .git directory in {dotGitDirectory}");

            using var repo = new Repository(dotGitDirectory);
            var result = repo.Info.WorkingDirectory;
            return result;
        }
    }
}
