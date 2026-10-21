using System.IO.Compression;

namespace Build.Tasks;

internal static class PackageContentsValidation
{
    public static void Validate(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        foreach (var entry in archive.Entries)
        {
            var path = entry.FullName.Replace('\\', '/');
            if (path.Contains("libgit2", StringComparison.OrdinalIgnoreCase)
                || path.Contains("runtimes/", StringComparison.OrdinalIgnoreCase) && path.Contains("/native/", StringComparison.OrdinalIgnoreCase))
            {
                throw new CakeException($"Native Git or runtime payload '{path}' is forbidden in {packagePath}.");
            }

            if (!path.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            using var reader = new StreamReader(entry.Open());
            if (reader.ReadToEnd().Contains("LibGit2Sharp", StringComparison.OrdinalIgnoreCase))
            {
                throw new CakeException($"LibGit2Sharp dependency remains in '{path}' in {packagePath}.");
            }
        }
    }
}
