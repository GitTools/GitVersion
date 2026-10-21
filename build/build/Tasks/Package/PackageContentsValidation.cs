using System.IO.Compression;
using System.Xml.Linq;

namespace Build.Tasks;

internal static class PackageContentsValidation
{
    public static void ValidatePackages(IEnumerable<string> toolPackages, IEnumerable<string> msBuildPackages)
    {
        var tools = toolPackages.ToArray();
        var msBuild = msBuildPackages.ToArray();
        if (tools.Length == 0 || msBuild.Length == 0)
        {
            throw new CakeException("Expected both GitVersion.Tool and GitVersion.MsBuild NuGet packages for validation.");
        }
        foreach (var package in tools.Concat(msBuild))
        {
            Validate(package);
        }
    }

    public static void Validate(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var nuspec = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        using var nuspecStream = nuspec.Open();
        var packageId = XDocument.Load(nuspecStream).Descendants().Single(element => element.Name.LocalName == "id").Value;
        var nativeTool = packageId.StartsWith("GitVersion.Tool.", StringComparison.OrdinalIgnoreCase);
        if (nativeTool && !archive.Entries.Any(entry => entry.FullName.Split('/') is ["tools", "any", _, "gitversion" or "gitversion.exe"]))
        {
            throw new CakeException($"Native CLI executable is missing from {packagePath}.");
        }
        foreach (var entry in archive.Entries)
        {
            var path = entry.FullName.Replace('\\', '/');
            if (nativeTool && path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                || path.Contains("libgit2", StringComparison.OrdinalIgnoreCase)
                || path.Contains("runtimes/", StringComparison.OrdinalIgnoreCase) && path.Contains("/native/", StringComparison.OrdinalIgnoreCase))
            {
                throw new CakeException($"Unsupported payload '{path}' in {packagePath}.");
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
