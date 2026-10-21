using NuGet.Packaging;

namespace Build.Tasks;

internal static class PackageContentsValidation
{
    public static void Validate(string packagePath)
    {
        using var package = new PackageArchiveReader(packagePath);
        var files = package.GetFiles().ToArray();
        var nuspecs = files.Where(path => path.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (nuspecs.Length != 1 || nuspecs[0].Replace('\\', '/').Contains('/'))
        {
            throw new CakeException($"Expected one root-level nuspec in {packagePath}.");
        }
        var nuspec = package.NuspecReader;
        // NuGet tolerates duplicate metadata/id nodes; reject ambiguous package identities.
        var root = nuspec.Xml.Root;
        var metadata = root?.Elements().Where(element => element.Name.LocalName == "metadata").ToArray();
        if (root?.Name.LocalName != "package" || metadata is not [var packageMetadata])
        {
            throw new CakeException($"Expected one package metadata element in {packagePath}.");
        }
        var ids = packageMetadata.Elements().Where(element => element.Name.LocalName == "id").ToArray();
        var packageId = nuspec.GetId();
        if (ids.Length != 1 || string.IsNullOrWhiteSpace(packageId))
        {
            throw new CakeException($"Expected one non-empty package id in {packagePath}.");
        }
        var nativeTool = packageId.StartsWith("GitVersion.Tool.", StringComparison.OrdinalIgnoreCase);
        var nativeRuntime = nativeTool ? packageId["GitVersion.Tool.".Length..] : null;
        if (nativeTool && !files.Any(path =>
                path.Replace('\\', '/').Split('/') is ["tools", "any", var runtime, "gitversion" or "gitversion.exe"]
                && string.Equals(runtime, nativeRuntime, StringComparison.OrdinalIgnoreCase)))
        {
            throw new CakeException($"Native CLI executable is missing from {packagePath}.");
        }
        foreach (var file in files)
        {
            var path = file.Replace('\\', '/');
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
            using var reader = new StreamReader(package.GetStream(file));
            if (reader.ReadToEnd().Contains("LibGit2Sharp", StringComparison.OrdinalIgnoreCase))
            {
                throw new CakeException($"LibGit2Sharp dependency remains in '{path}' in {packagePath}.");
            }
        }
    }
}
