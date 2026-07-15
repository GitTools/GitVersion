using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

/// <summary>
/// The backend-agnostic part of resolving the repository paths GitVersion works with
/// (<see cref="IGitRepositoryInfo"/>): the dot-git directory with its worktree handling,
/// the project root, and the git root. Each Git backend supplies its own repository
/// discovery and working-directory resolution.
/// </summary>
internal static class RepositoryPathResolution
{
    private const string DotGitDirectoryNotFoundMessage = "Cannot find the .git directory";

    /// <summary>
    /// Resolves the dot-git directory: the dynamic repository path when one is configured,
    /// otherwise the discovered git directory — mapped to the main repository's directory
    /// when it is a linked worktree's.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="dynamicGitRepositoryPath">The dynamic repository path, when configured.</param>
    /// <param name="workingDirectory">The working directory to discover from.</param>
    /// <param name="discoverGitDirectory">Discovers the git directory containing a path, or <see langword="null"/>.</param>
    public static string? ResolveDotGitDirectory(
        IFileSystem fileSystem,
        string? dynamicGitRepositoryPath,
        string workingDirectory,
        Func<string, string?> discoverGitDirectory)
    {
        var gitDirectory = (!dynamicGitRepositoryPath.IsNullOrWhiteSpace()
                ? dynamicGitRepositoryPath
                : discoverGitDirectory(workingDirectory))
            ?.TrimEnd('/', '\\');

        if (string.IsNullOrEmpty(gitDirectory))
        {
            throw new DirectoryNotFoundException(DotGitDirectoryNotFoundMessage);
        }

        var directoryInfo = fileSystem.Directory.GetParent(gitDirectory) ?? throw new DirectoryNotFoundException(DotGitDirectoryNotFoundMessage);
        return gitDirectory.Contains(FileSystemHelper.Path.Combine(".git", "worktrees"))
            ? fileSystem.Directory.GetParent(directoryInfo.FullName)?.FullName
            : gitDirectory;
    }

    /// <summary>
    /// Resolves the project root: the working directory itself for dynamic repositories,
    /// otherwise the discovered repository's working directory (with a trailing separator,
    /// as libgit2 reports it).
    /// </summary>
    /// <param name="dynamicGitRepositoryPath">The dynamic repository path, when configured.</param>
    /// <param name="workingDirectory">The working directory to discover from.</param>
    /// <param name="resolveRepositoryWorkingDirectory">Resolves the working directory of the repository containing a path, or <see langword="null"/>.</param>
    public static string ResolveProjectRootDirectory(
        string? dynamicGitRepositoryPath,
        string workingDirectory,
        Func<string, string?> resolveRepositoryWorkingDirectory)
    {
        if (!dynamicGitRepositoryPath.IsNullOrWhiteSpace())
        {
            return workingDirectory;
        }

        return resolveRepositoryWorkingDirectory(workingDirectory)
            ?? throw new DirectoryNotFoundException(DotGitDirectoryNotFoundMessage);
    }

    /// <summary>
    /// Resolves the git root: the dot-git directory for dynamic repositories, otherwise
    /// the project root.
    /// </summary>
    /// <param name="dynamicGitRepositoryPath">The dynamic repository path, when configured.</param>
    /// <param name="dotGitDirectory">The resolved dot-git directory.</param>
    /// <param name="projectRootDirectory">The resolved project root.</param>
    public static string? ResolveGitRootPath(string? dynamicGitRepositoryPath, string? dotGitDirectory, string? projectRootDirectory) =>
        !dynamicGitRepositoryPath.IsNullOrWhiteSpace() ? dotGitDirectory : projectRootDirectory;
}
