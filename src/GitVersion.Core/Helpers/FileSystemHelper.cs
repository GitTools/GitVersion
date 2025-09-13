using System.IO.Abstractions;

namespace GitVersion.Helpers;

internal static class FileSystemHelper
{
    private static readonly FileSystem fileSystem = new();

    internal static class File
    {
        public static bool Exists(string path) => fileSystem.File.Exists(path);
        public static void Delete(string path) => fileSystem.File.Delete(path);
        public static string ReadAllText(string path) => fileSystem.File.ReadAllText(path);
        public static void WriteAllText(string path, string? contents) => fileSystem.File.WriteAllText(path, contents);
    }

    internal static class Directory
    {
        public static bool Exists(string directoryPath) => fileSystem.Directory.Exists(directoryPath);

        public static void DeleteDirectory(string directoryPath)
        {
            // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502

            if (!fileSystem.Directory.Exists(directoryPath))
            {
                Trace.WriteLine($"Directory '{directoryPath}' is missing and can't be removed.");

                return;
            }

            var files = fileSystem.Directory.GetFiles(directoryPath);
            var dirs = fileSystem.Directory.GetDirectories(directoryPath);

            foreach (var file in files)
            {
                fileSystem.File.SetAttributes(file, FileAttributes.Normal);
                fileSystem.File.Delete(file);
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir);
            }

            fileSystem.File.SetAttributes(directoryPath, FileAttributes.Normal);
            try
            {
                fileSystem.Directory.Delete(directoryPath, false);
            }
            catch (IOException)
            {
                Trace.WriteLine(string.Format("{0}The directory '{1}' could not be deleted!" +
                                              "{0}Most of the time, this is due to an external process accessing the files in the temporary repositories created during the test runs, and keeping a handle on the directory, thus preventing the deletion of those files." +
                                              "{0}Known and common causes include:" +
                                              "{0}- Windows Search Indexer (go to the Indexing Options, in the Windows Control Panel, and exclude the bin folder of LibGit2Sharp.Tests)" +
                                              "{0}- Antivirus (exclude the bin folder of LibGit2Sharp.Tests from the paths scanned by your real-time antivirus){0}",
                    Path.NewLine, fileSystem.Path.GetFullPath(directoryPath)));
            }
        }
    }

    internal static class Path
    {
        public static string NewLine => SysEnv.NewLine;
        public static char DirectorySeparatorChar => fileSystem.Path.DirectorySeparatorChar;

        private static readonly StringComparison OsDependentComparison =
            OperatingSystem.IsLinux()
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

        public static string GetCurrentDirectory() => AppContext.BaseDirectory ?? throw new InvalidOperationException();

        public static string GetTempPathLegacy() => fileSystem.Path.GetTempPath().TrimEnd(DirectorySeparatorChar);
        public static string GetTempPath()
        {
            var tempPath = GetCurrentDirectory();
            if (!string.IsNullOrWhiteSpace(SysEnv.GetEnvironmentVariable("RUNNER_TEMP")))
            {
                tempPath = SysEnv.GetEnvironmentVariable("RUNNER_TEMP");
            }

            return tempPath!;
        }

        public static string GetRepositoryTempPath() => Combine(GetTempPath(), "TestRepositories", Guid.NewGuid().ToString());

        public static string GetDirectoryName(string? path)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));

            return fileSystem.Path.GetDirectoryName(path)!;
        }

        public static string GetFileName(string? path)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));

            return fileSystem.Path.GetFileName(path);
        }

        public static string? GetFileNameWithoutExtension(string? path) => fileSystem.Path.GetFileNameWithoutExtension(path);

        public static string? GetExtension(string? path) => fileSystem.Path.GetExtension(path);

        public static string GetFullPath(string? path) => fileSystem.Path.GetFullPath(path!);

        public static string Combine(string? path1, string? path2)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path1);
            ArgumentException.ThrowIfNullOrWhiteSpace(path2);

            return fileSystem.Path.Combine(path1, path2);
        }

        public static string Combine(string? path1)
        {
            ArgumentNullException.ThrowIfNull(path1, nameof(path1));

            return fileSystem.Path.Combine(path1);
        }

        public static string Combine(string? path1, string? path2, string? path3)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path1);
            ArgumentException.ThrowIfNullOrWhiteSpace(path2);
            ArgumentException.ThrowIfNullOrWhiteSpace(path3);

            return fileSystem.Path.Combine(path1, path2, path3);
        }

        public static string Combine(string? path1, string? path2, string? path3, string? path4)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path1);
            ArgumentException.ThrowIfNullOrWhiteSpace(path2);
            ArgumentException.ThrowIfNullOrWhiteSpace(path3);
            ArgumentException.ThrowIfNullOrWhiteSpace(path4);

            return fileSystem.Path.Combine(path1, path2, path3, path4);
        }

        public static bool Equal(string? path, string? otherPath) =>
            string.Equals(
                GetFullPath(path).TrimEnd('\\').TrimEnd('/'),
                GetFullPath(otherPath).TrimEnd('\\').TrimEnd('/'),
                OsDependentComparison);

        public static string GetRandomFileName() => fileSystem.Path.GetRandomFileName();

        public static bool IsPathRooted(string? path) => fileSystem.Path.IsPathRooted(path);
    }
}
