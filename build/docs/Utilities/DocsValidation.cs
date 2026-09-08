using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Path = System.IO.Path;

namespace Docs.Utilities;

public static class DocsValidation
{
    private const string IndexPage = "index.html";
    private const string Stylesheet = "assets/css/redesign.css";

    public static void Validate(string output, PreparedEdition[] editions)
    {
        var errors = new HashSet<string>();
        foreach (var prepared in editions)
        {
            ValidateEdition(output, editions[0], prepared, errors);
        }

        ValidateDefaultRedirects(output, editions[0].Edition.BasePath, errors);
        var parser = new HtmlParser();
        foreach (var file in Directory.GetFiles(output, "*.html", SearchOption.AllDirectories))
        {
            var document = parser.ParseDocument(File.ReadAllText(file));
            ValidateRedirects(output, editions, file, document, errors);
            ValidateLinks(output, file, document, errors);
        }
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Documentation validation failed:\n" + string.Join('\n', errors.Take(30)));
        }
    }

    private static void ValidateEdition(string output, PreparedEdition latest, PreparedEdition prepared, HashSet<string> errors)
    {
        var directory = output + prepared.Edition.BasePath;
        if (!File.Exists(Path.Combine(prepared.AssemblyRoot!, "GitVersion.MsBuild.dll")))
        {
            errors.Add($"Missing MSBuild API assembly: {prepared.Edition.Id}");
        }

        foreach (var page in new[] { IndexPage, "docs/index.html", "api/index.html", "assets/js/versions.js", Stylesheet }
                     .Where(page => !File.Exists(Path.Combine(directory, page))))
        {
            errors.Add($"Missing {prepared.Edition.Id}/{page}");
        }

        if (!File.ReadAllBytes(Path.Combine(directory, Stylesheet)).SequenceEqual(File.ReadAllBytes(Path.Combine(output + latest.Edition.BasePath, Stylesheet))))
        {
            errors.Add($"{prepared.Edition.Id} uses a different design.");
        }

        ValidateApi(directory, prepared, errors);
    }

    private static void ValidateApi(string directory, PreparedEdition prepared, HashSet<string> errors)
    {
        if (prepared.AssemblyRoot is not null)
        {
            foreach (var route in File.ReadAllLines(Path.Combine(prepared.AssemblyRoot, "public-type-routes.txt"))
                         .Where(route => !File.Exists(Path.Combine(directory, "api", route))))
            {
                errors.Add($"Missing published API type: {prepared.Edition.Id}/{route}");
            }
        }
        foreach (var assemblyPage in Directory.GetDirectories(Path.Combine(directory, "api"), "*.dll")
                     .Where(page => !Path.GetFileName(page).StartsWith("GitVersion", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"Unexpected dependency assembly page: {prepared.Edition.Id}/{Path.GetFileName(assemblyPage)}");
        }
    }

    private static void ValidateDefaultRedirects(string output, string basePath, HashSet<string> errors)
    {
        foreach (var required in new[] { IndexPage, "docs/index.html", "api/index.html" })
        {
            var path = Path.Combine(output, required);
            if (!File.Exists(path) || !File.ReadAllText(path).Contains("url=" + basePath + "/", StringComparison.Ordinal))
            {
                errors.Add($"Missing default edition redirect: {required}");
            }
        }
    }

    private static void ValidateRedirects(string output, PreparedEdition[] editions, string file, IDocument document, HashSet<string> errors)
    {
        var edition = editions.FirstOrDefault(e => new[] { e.Edition.BasePath }.Concat(e.Edition.Aliases ?? [])
            .Any(prefix => file.StartsWith(output + prefix + Path.DirectorySeparatorChar, StringComparison.Ordinal)));
        foreach (var target in document.QuerySelectorAll("meta[http-equiv]").Select(DocsLinks.RefreshTarget).OfType<string>().Where(DocsLinks.IsRootRelative))
        {
            if (edition is not null && !target.StartsWith(edition.Edition.BasePath + "/", StringComparison.Ordinal))
            {
                errors.Add($"Redirect leaves edition: {Path.GetRelativePath(output, file)}: {target}");
            }

            if (!TargetExists(output + DocsLinks.PathOnly(target)))
            {
                errors.Add($"Broken redirect: {Path.GetRelativePath(output, file)}: {target}");
            }
        }
    }

    private static void ValidateLinks(string output, string file, IDocument document, HashSet<string> errors)
    {
        var links = document.QuerySelectorAll("[href], [src]")
            .SelectMany(element => new[] { element.GetAttribute("href"), element.GetAttribute("src") })
            .OfType<string>().Where(IsLocalLink);
        foreach (var value in links)
        {
            var path = Uri.UnescapeDataString(DocsLinks.PathOnly(value));
            var target = path.StartsWith('/') ? output + path : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, path));
            if (!TargetExists(target))
            {
                errors.Add($"{Path.GetRelativePath(output, file)}: {value}");
            }
        }
    }

    private static bool IsLocalLink(string value) => !string.IsNullOrEmpty(value) && !value.StartsWith('#')
        && !value.StartsWith("//", StringComparison.Ordinal)
        && !(Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme != "file");

    private static bool TargetExists(string target) => File.Exists(target) || File.Exists(target + ".html")
        || File.Exists(Path.Combine(target, IndexPage));
}
