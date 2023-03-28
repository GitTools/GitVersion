using GitVersion.Extensions;

namespace GitVersion.Helpers;

public static class PathHelper
{
    public static string GetFullPath(string? path)
    {
        path.NotNull();

        return Path.GetFullPath(path);
    }
    public static string Combine(string? path1, string? path2)
    {
        if (path1 == null || path2 == null)
            throw new ArgumentNullException((path1 == null) ? nameof(path1) : nameof(path2));

        return Path.Combine(path1, path2);
    }

    public static string Combine(string? path1)
    {
        path1.NotNull();

        return Path.Combine(path1);
    }

    public static string Combine(string? path1, string? path2, string? path3)
    {
        if (path1 == null || path2 == null || path3 == null)
            throw new ArgumentNullException((path1 == null) ? nameof(path1) : (path2 == null) ? nameof(path2) : nameof(path3));

        return Path.Combine(path1, path2, path3);
    }

    public static string Combine(string? path1, string? path2, string? path3, string? path4)
    {
        if (path1 == null || path2 == null || path3 == null || path4 == null)
            throw new ArgumentNullException((path1 == null) ? nameof(path1) : (path2 == null) ? nameof(path2) : (path3 == null) ? nameof(path3) : nameof(path4));

        return Path.Combine(path1, path2, path3, path4);
    }
}
