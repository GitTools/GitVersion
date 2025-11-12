using System.IO.Abstractions;
using GitVersion.Helpers;

namespace GitVersion.Extensions;

public static class FileSystemExtensions
{
    extension(IFileSystem fileSystem)
    {
        public long GetLastDirectoryWrite(string path) => fileSystem.DirectoryInfo.New(path)
            .GetDirectories("*.*", SearchOption.AllDirectories)
            .Select(d => d.LastWriteTimeUtc)
            .DefaultIfEmpty()
            .Max()
            .Ticks;

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
