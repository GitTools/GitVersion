using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

/// <summary>
/// Computes the path of the dynamically created Git repository used for remote-URL
/// scenarios. Both Git backends share this logic and differ only in how they check
/// whether an existing directory is a repository with a matching remote.
/// </summary>
internal static class DynamicRepositoryPath
{
    private static readonly char[] DirectorySeparators = ['/', '\\'];

    /// <summary>
    /// Gets the path of the dynamic repository for <paramref name="targetUrl"/>, or
    /// <see langword="null"/> when no target URL is configured.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="targetUrl">The URL of the remote repository.</param>
    /// <param name="clonePath">The directory to clone into, or <see langword="null"/> for the temp directory.</param>
    /// <param name="gitRepoHasMatchingRemote">Checks whether the repository at the given path has a remote with the given URL.</param>
    public static string? Get(IFileSystem fileSystem, string? targetUrl, string? clonePath, Func<string, string, bool> gitRepoHasMatchingRemote)
    {
        if (targetUrl.IsNullOrWhiteSpace())
        {
            return null;
        }

        var userTemp = clonePath ?? FileSystemHelper.Path.GetTempPath();
        var repositoryName = targetUrl.Split(DirectorySeparators)[^1].Replace(".git", string.Empty);
        var possiblePath = FileSystemHelper.Path.Combine(userTemp, repositoryName);

        // Verify that the existing directory is ok for us to use
        if (fileSystem.Directory.Exists(possiblePath) && !gitRepoHasMatchingRemote(possiblePath, targetUrl))
        {
            var i = 1;
            var originalPath = possiblePath;
            bool possiblePathExists;
            do
            {
                possiblePath = $"{originalPath}_{i++}";
                possiblePathExists = fileSystem.Directory.Exists(possiblePath);
            } while (possiblePathExists && !gitRepoHasMatchingRemote(possiblePath, targetUrl));
        }

        var repositoryPath = FileSystemHelper.Path.Combine(possiblePath, ".git");
        return repositoryPath;
    }
}
