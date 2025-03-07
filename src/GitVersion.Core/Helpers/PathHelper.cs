using System.Runtime.InteropServices;

namespace GitVersion.Helpers;

internal static class PathHelper
{
    public static string NewLine => SysEnv.NewLine;
    public static char DirectorySeparatorChar => Path.DirectorySeparatorChar;

    private static readonly StringComparison OsDependentComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

    public static string GetCurrentDirectory() => AppContext.BaseDirectory ?? throw new InvalidOperationException();

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

        return Path.GetDirectoryName(path)!;
    }

    public static string GetFileName(string? path)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));

        return Path.GetFileName(path);
    }

    public static string? GetFileNameWithoutExtension(string? path) => Path.GetFileNameWithoutExtension(path);

    public static string? GetExtension(string? path) => Path.GetExtension(path);

    public static string GetFullPath(string? path) => Path.GetFullPath(path!);

    public static string Combine(string? path1, string? path2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path1);
        ArgumentException.ThrowIfNullOrWhiteSpace(path2);

        return Path.Combine(path1, path2);
    }

    public static string Combine(string? path1)
    {
        ArgumentNullException.ThrowIfNull(path1, nameof(path1));

        return Path.Combine(path1);
    }

    public static string Combine(string? path1, string? path2, string? path3)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path1);
        ArgumentException.ThrowIfNullOrWhiteSpace(path2);
        ArgumentException.ThrowIfNullOrWhiteSpace(path3);

        return Path.Combine(path1, path2, path3);
    }

    public static string Combine(string? path1, string? path2, string? path3, string? path4)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path1);
        ArgumentException.ThrowIfNullOrWhiteSpace(path2);
        ArgumentException.ThrowIfNullOrWhiteSpace(path3);
        ArgumentException.ThrowIfNullOrWhiteSpace(path4);

        return Path.Combine(path1, path2, path3, path4);
    }

    public static bool Equal(string? path, string? otherPath) =>
        string.Equals(
            GetFullPath(path).TrimEnd('\\').TrimEnd('/'),
            GetFullPath(otherPath).TrimEnd('\\').TrimEnd('/'),
            OsDependentComparison);

    public static string GetRandomFileName() => Path.GetRandomFileName();

    public static string GetTempFileName() => Path.GetTempFileName();

    public static bool IsPathRooted(string? path) => Path.IsPathRooted(path);
}
