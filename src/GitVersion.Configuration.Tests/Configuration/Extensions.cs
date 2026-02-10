using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Configuration.Tests;

public static class Extensions
{
    extension(IFileSystem fileSystem)
    {
        public IDisposable<string> SetupConfigFile(string? path = null, string fileName = ConfigurationFileLocator.DefaultFileName, string text = "")
        {
            if (path.IsNullOrEmpty())
            {
                path = FileSystemHelper.Path.GetRepositoryTempPath();
            }

            var fullPath = FileSystemHelper.Path.Combine(path, fileName);
            var directory = FileSystemHelper.Path.GetDirectoryName(fullPath);
            if (!fileSystem.Directory.Exists(directory))
            {
                fileSystem.Directory.CreateDirectory(directory);
            }

            fileSystem.File.WriteAllText(fullPath, text);

            return Disposable.Create(fullPath, () =>
            {
                fileSystem.File.Delete(fullPath);
                fileSystem.Directory.Delete(directory, true);
            });
        }
    }
}
