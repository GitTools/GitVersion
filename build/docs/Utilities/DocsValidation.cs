using AngleSharp.Html.Parser;
using Path = System.IO.Path;

namespace Docs.Utilities;

public static class DocsValidation
{
    public static void Validate(string output, PreparedEdition[] editions)
    {
        var errors = new HashSet<string>();
        var parser = new HtmlParser();
        foreach (var prepared in editions)
        {
            var directory = output + prepared.Edition.BasePath;
            foreach (var page in new[] { "index.html", "docs/index.html", "api/index.html", "assets/js/versions.js", "assets/css/redesign.css" })
                if (!File.Exists(Path.Combine(directory, page))) errors.Add($"Missing {prepared.Edition.Id}/{page}");
            if (!File.ReadAllBytes(Path.Combine(directory, "assets/css/redesign.css")).SequenceEqual(File.ReadAllBytes(Path.Combine(output, "assets/css/redesign.css"))))
                errors.Add($"{prepared.Edition.Id} uses a different design.");
            if (prepared.AssemblyRoot is not null)
            foreach (var route in File.ReadAllLines(Path.Combine(prepared.AssemblyRoot, "public-type-routes.txt")))
                if (!File.Exists(Path.Combine(directory, "api", route))) errors.Add($"Missing published API type: {prepared.Edition.Id}/{route}");
            foreach (var assemblyPage in Directory.GetDirectories(Path.Combine(directory, "api"), "*.dll"))
                if (!Path.GetFileName(assemblyPage).StartsWith("GitVersion", StringComparison.OrdinalIgnoreCase))
                    errors.Add($"Unexpected dependency assembly page: {prepared.Edition.Id}/{Path.GetFileName(assemblyPage)}");
        }
        foreach (var file in Directory.GetFiles(output, "*.html", SearchOption.AllDirectories))
        {
            var document = parser.ParseDocument(File.ReadAllText(file));
            foreach (var element in document.QuerySelectorAll("[href], [src]"))
            foreach (var attribute in new[] { "href", "src" })
            {
                var value = element.GetAttribute(attribute);
                if (string.IsNullOrEmpty(value) || value.StartsWith('#') || value.StartsWith("//", StringComparison.Ordinal)
                    || Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme != "file") continue;
                var path = Uri.UnescapeDataString(value.Split('#', '?')[0]);
                var target = path.StartsWith('/') ? output + path : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, path));
                if (!File.Exists(target) && !File.Exists(target + ".html") && !File.Exists(Path.Combine(target, "index.html")))
                    errors.Add($"{Path.GetRelativePath(output, file)}: {value}");
            }
        }
        if (errors.Count > 0) throw new InvalidOperationException("Documentation validation failed:\n" + string.Join('\n', errors.Take(30)));
    }
}
