using System.IO.Abstractions;
using GitVersion.Helpers;

namespace GitVersion.Extensions;

/// <summary>Extension methods on <see cref="IFileSystem"/> for common file-system operations.</summary>
public static class FileSystemExtensions
{
    extension(IFileSystem fileSystem)
    {
        /// <summary>Returns the latest last-write timestamp (in UTC ticks) of any subdirectory under <paramref name="path"/>.</summary>
        public long GetLastDirectoryWrite(string path) => fileSystem.DirectoryInfo.New(path)
            .GetDirectories("*.*", SearchOption.AllDirectories)
            .Select(d => d.LastWriteTimeUtc)
            .DefaultIfEmpty()
            .Max()
            .Ticks;

        /// <summary>Walks up the directory tree from <paramref name="path"/> looking for a <c>.git</c> directory or file, returning the git directory and working-tree paths when found.</summary>
        public (string GitDirectory, string WorkingTreeDirectory)? FindGitDir(string? path)
        {
            var startingDir = path;
            while (startingDir is not null)
            {
                var dirOrFilePath = FileSystemHelper.Path.Combine(startingDir, ".git");
                if (fileSystem.Directory.Exists(dirOrFilePath))
                {
                    return (dirOrFilePath, FileSystemHelper.Path.GetDirectoryName(dirOrFilePath));
                }

                if (fileSystem.File.Exists(dirOrFilePath))
                {
                    var relativeGitDirPath = ReadGitDirFromFile(dirOrFilePath);
                    if (!string.IsNullOrWhiteSpace(relativeGitDirPath))
                    {
                        var fullGitDirPath = FileSystemHelper.Path.GetFullPath(FileSystemHelper.Path.Combine(startingDir, relativeGitDirPath));
                        if (fileSystem.Directory.Exists(fullGitDirPath))
                        {
                            return (fullGitDirPath, FileSystemHelper.Path.GetDirectoryName(dirOrFilePath));
                        }
                    }
                }

                startingDir = FileSystemHelper.Path.GetDirectoryName(startingDir);
            }

            return null;

            string? ReadGitDirFromFile(string fileName)
            {
                const string expectedPrefix = "gitdir: ";
                var firstLineOfFile = fileSystem.File.ReadLines(fileName).FirstOrDefault();
                if (firstLineOfFile?.StartsWith(expectedPrefix) ?? false)
                {
                    return firstLineOfFile[expectedPrefix.Length..]; // strip off the prefix, leaving just the path
                }

                return null;
            }
        }
    }
}
