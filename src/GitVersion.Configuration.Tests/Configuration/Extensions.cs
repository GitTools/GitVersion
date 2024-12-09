using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Configuration.Tests.Configuration;

public static class Extensions
{
    public static IDisposable<string> SetupConfigFile(this IFileSystem fileSystem, string? path = null, string fileName = ConfigurationFileLocator.DefaultFileName, string text = "")
    {
        if (path.IsNullOrEmpty())
        {
            path = PathHelper.GetRepositoryTempPath();
        }

        var fullPath = PathHelper.Combine(path, fileName);
        var directory = PathHelper.GetDirectoryName(fullPath);
        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        fileSystem.WriteAllText(fullPath, text);

        return Disposable.Create(fullPath, () => fileSystem.Delete(fullPath));
    }
}
