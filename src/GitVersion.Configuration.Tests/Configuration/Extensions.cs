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
        fileSystem.WriteAllText(fullPath, text);

        return Disposable.Create(fullPath, () => fileSystem.Delete(fullPath));
    }
}
