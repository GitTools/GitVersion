using System.IO.Compression;
using System.Xml.Linq;

namespace Build.Tasks;

internal static class PackageContentsValidation
{
    public static void Validate(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var nuspecs = archive.Entries.Where(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (nuspecs.Length != 1 || nuspecs[0].FullName.Replace('\\', '/').Contains('/'))
        {
            throw new CakeException($"Expected one root-level nuspec in {packagePath}.");
        }
        using var nuspecStream = nuspecs[0].Open();
        var root = XDocument.Load(nuspecStream).Root;
        var metadata = root?.Elements().Where(element => element.Name.LocalName == "metadata").ToArray();
        if (root?.Name.LocalName != "package" || metadata is not [var packageMetadata])
        {
            throw new CakeException($"Expected one package metadata element in {packagePath}.");
        }
        var ids = packageMetadata.Elements().Where(element => element.Name.LocalName == "id").ToArray();
        if (ids is not [var id] || string.IsNullOrWhiteSpace(id.Value))
        {
            throw new CakeException($"Expected one non-empty package id in {packagePath}.");
        }
        var packageId = id.Value;
        var nativeTool = packageId.StartsWith("GitVersion.Tool.", StringComparison.OrdinalIgnoreCase);
        var nativeRuntime = nativeTool ? packageId["GitVersion.Tool.".Length..] : null;
        if (nativeTool && !archive.Entries.Any(entry =>
                entry.FullName.Replace('\\', '/').Split('/') is ["tools", "any", var runtime, "gitversion" or "gitversion.exe"]
                && string.Equals(runtime, nativeRuntime, StringComparison.OrdinalIgnoreCase)))
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
