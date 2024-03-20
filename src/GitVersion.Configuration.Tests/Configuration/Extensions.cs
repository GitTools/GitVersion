using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Configuration.Tests.Configuration;

public static class Extensions
{
    public static string SetupConfigFile(this IFileSystem fileSystem, string text, string? path = null, string fileName = ConfigurationFileLocator.DefaultFileName)
    {
        if (path.IsNullOrEmpty())
        {
            path = PathHelper.GetRepositoryTempPath();
        }

        var fullPath = PathHelper.Combine(path, fileName);
        fileSystem.WriteAllText(fullPath, text);

        return fullPath;
    }
}
