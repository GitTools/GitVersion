using Cake.Wyam;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Path = System.IO.Path;

namespace Docs.Utilities;

public static class VersionedDocs
{
    public static void Build(BuildContext context)
    {
        var root = context.Environment.WorkingDirectory.FullPath;
        var editions = context.DocumentationInputs ?? throw new InvalidOperationException("Documentation inputs were not prepared.");
        var currentIndex = Array.FindIndex(editions, p => p.Edition.Id == "current");
        var currentAssemblies = Path.Combine(root, "artifacts/docs/inputs/current/assemblies");
        if (Directory.Exists(currentAssemblies)) Directory.Delete(currentAssemblies, true);
        foreach (var project in new[] { "GitVersion.App", "GitVersion.MsBuild" })
            context.DotNetBuild(Path.Combine(root, "src", project, project + ".csproj"), new()
            {
                Configuration = "Debug",
                OutputDirectory = currentAssemblies
            });
        DocsApi.Prepare(root, currentAssemblies);
        editions[currentIndex] = editions[currentIndex] with { AssemblyRoot = currentAssemblies };
        File.WriteAllText(Path.Combine(root, "artifacts/docs/inputs.json"), JsonSerializer.Serialize(editions, DocsInputs.Json));
        var output = Path.Combine(root, "artifacts/docs/preview.new");
        if (Directory.Exists(output)) Directory.Delete(output, true);
        Directory.CreateDirectory(output);
        context.StageMermaidRuntimeForWyam();
        var assetVersion = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            File.ReadAllBytes(Path.Combine(root, "docs/input/assets/css/redesign.css"))))[..12];
        foreach (var prepared in editions)
        {
            var edition = prepared.Edition;
            var work = Path.Combine(root, "artifacts/docs/work", edition.Id);
            var input = Path.Combine(work, "input");
            if (Directory.Exists(input)) Directory.Delete(input, true);
            DocsInputs.CopyTree(Path.Combine(prepared.ContentRoot, "docs/input"), input);
            var diagrams = Path.Combine(prepared.ContentRoot, "docs/diagrams");
            if (Directory.Exists(diagrams)) DocsInputs.CopyTree(diagrams, Path.Combine(work, "diagrams"));
            // Presentation belongs to the current checkout; prose belongs to its release.
            foreach (var file in Directory.GetFiles(Path.Combine(root, "docs/input"), "_*.cshtml"))
                File.Copy(file, Path.Combine(input, Path.GetFileName(file)), true);
            File.Copy(Path.Combine(root, "docs/input/index.cshtml"), Path.Combine(input, "index.cshtml"), true);
            DocsInputs.CopyTree(Path.Combine(root, "docs/input/Shared"), Path.Combine(input, "Shared"));
            DocsInputs.CopyTree(Path.Combine(root, "docs/input/assets"), Path.Combine(input, "assets"));
            var theme = Path.Combine(work, "theme");
            if (Directory.Exists(theme)) Directory.Delete(theme, true);
            DocsInputs.CopyTree(Path.Combine(root, "docs/theme"), theme);
            var config = new List<string>
            {
                "Settings[\"Title\"] = \"GitVersion\";",
                "Settings[\"IncludeGlobalNamespace\"] = false;",
                "Settings[\"IgnoreFolders\"] = \"**/mdsource\";",
                $"Settings[\"DocsEdition\"] = {Literal(edition.Label)};",
                $"Settings[\"DocsAssetVersion\"] = {Literal(assetVersion)};",
                $"Settings[\"DocsRelease\"] = {Literal(prepared.Release)};",
                $"Settings[\"BaseEditUrl\"] = {Literal("https://github.com/GitTools/GitVersion/tree/" + (edition.Id == "current" ? "main" : prepared.SourceCommit) + "/docs/input/")};"
            };
            config.Add("Settings[\"SourceFiles\"] = new string[0];");
            config.Add("Pipelines[\"Api\"].Clear();");
            // Keep dependencies available for type resolution, but only publish our assembly symbols.
            config.Add($"Pipelines[\"Api\"].Add(new AnalyzeCSharp().WherePublic().WhereNamespaces(false, \"GitVersion\").WithAssemblies({Literal(prepared.AssemblyRoot + "/*.dll")}).WithAssemblySymbols().WithWritePathPrefix(\"api\"));");
            config.Add("Pipelines[\"Api\"].Add(new Where((document, context) => document.String(CodeAnalysisKeys.Kind) != \"Assembly\" || document.String(CodeAnalysisKeys.DisplayName).StartsWith(\"GitVersion\", StringComparison.OrdinalIgnoreCase)));");
            File.WriteAllLines(Path.Combine(work, "config.wyam"), config);
            var destination = output + edition.BasePath;
            context.Wyam(new WyamSettings
            {
                RootPath = work,
                Recipe = "Docs",
                Theme = "Samson",
                OutputPath = destination,
                EnvironmentVariables = new Dictionary<string, string> { ["DOTNET_ROLL_FORWARD"] = "Major" }
            });
            PrefixLinks(destination, edition.BasePath);
        }
        var routes = editions.Select(p => new
        {
            id = p.Edition.Id, label = p.Edition.Label, basePath = p.Edition.BasePath,
            routes = Directory.GetFiles(output + p.Edition.BasePath, "*.html", SearchOption.AllDirectories)
                .Select(f => "/" + Path.GetRelativePath(output + p.Edition.BasePath, f).Replace('\\', '/'))
                .Select(p => p.EndsWith("/index.html", StringComparison.Ordinal) ? p[..^10] : p[..^5]).ToArray()
        }).ToArray();
        File.WriteAllText(Path.Combine(output, "versions.json"), JsonSerializer.Serialize(routes, DocsInputs.Json));
        File.WriteAllText(Path.Combine(output, "build-inputs.json"), JsonSerializer.Serialize(editions.Select(p => new
        {
            p.Edition, p.Release, p.SourceCommit, p.Packages, p.Dependencies
        }), DocsInputs.Json));
        var schemas = Path.Combine(root, "schemas");
        if (Directory.Exists(schemas)) DocsInputs.CopyTree(schemas, Path.Combine(output, "schemas"));
        foreach (var prepared in editions)
        foreach (var alias in prepared.Edition.Aliases ?? [])
        foreach (var route in routes.Single(r => r.id == prepared.Edition.Id).routes)
        {
            var target = prepared.Edition.BasePath + route;
            var path = output + alias + route + (route.EndsWith('/') ? "index.html" : ".html");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $"<!doctype html><meta charset=\"utf-8\"><script>location.replace({Literal(target)} + location.search + location.hash);</script><meta http-equiv=\"refresh\" content=\"0;url={target}\"><link rel=\"canonical\" href=\"{target}\"><a href=\"{target}\">Continue to {prepared.Edition.Label}</a>");
        }
        DocsValidation.Validate(output, editions);
        var preview = Path.Combine(root, "artifacts/docs/preview");
        var previous = preview + ".previous";
        if (Directory.Exists(previous)) Directory.Delete(previous, true);
        if (Directory.Exists(preview)) Directory.Move(preview, previous);
        try { Directory.Move(output, preview); }
        catch
        {
            if (Directory.Exists(previous)) Directory.Move(previous, preview);
            throw;
        }
        if (Directory.Exists(previous)) Directory.Delete(previous, true);
    }

    private static string Literal(string value) => JsonSerializer.Serialize(value);

    private static void PrefixLinks(string destination, string prefix)
    {
        var parser = new HtmlParser();
        foreach (var file in Directory.GetFiles(destination, "*.html", SearchOption.AllDirectories))
        {
            var document = parser.ParseDocument(File.ReadAllText(file));
            foreach (var element in document.QuerySelectorAll("[href], [src], [action]"))
            foreach (var attribute in new[] { "href", "src", "action" })
            {
                var value = element.GetAttribute(attribute);
                // These stale links exist in the tagged prose; keep corrections separate from the archive.
                if (value == "/docs/reference/modes/mainline") value = "/docs/reference/configuration#strategies";
                if (value is "/docs/workflows/GitFlow/v1.json" or "/docs/workflows/GitHubFlow/v1.json") value = value[..^5] + ".yml";
                if (value is not null && value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal))
                    element.SetAttribute(attribute, prefix + value);
            }
            File.WriteAllText(file, "<!DOCTYPE html>\n" + document.DocumentElement.OuterHtml);
        }
        // The search index contains root-relative URLs, independent of the HTML pipeline.
        foreach (var file in Directory.GetFiles(destination, "*searchIndex.js", SearchOption.AllDirectories))
            File.WriteAllText(file, File.ReadAllText(file).Replace("url:'/", "url:'" + prefix + "/", StringComparison.Ordinal));
    }

    public static void Preview(BuildContext context)
    {
        var root = context.Environment.WorkingDirectory.FullPath;
        var gate = new object();
        using var timer = new Timer(_ =>
        {
            lock (gate)
            {
                try
                {
                    context.Information("Documentation changed; rebuilding all editions...");
                    context.DocumentationInputs = new DocsInputs(root, message => context.Information(message)).Prepare().GetAwaiter().GetResult();
                    Build(context);
                    context.Information("Documentation rebuilt. Refresh the browser to see the changes.");
                }
                catch (Exception e) { context.Error($"Documentation rebuild failed: {e.Message}"); }
            }
        });
        using var watcher = new FileSystemWatcher(Path.Combine(root, "docs")) { IncludeSubdirectories = true, EnableRaisingEvents = true };
        void Changed(object sender, FileSystemEventArgs args)
        {
            var path = Path.GetRelativePath(Path.Combine(root, "docs"), args.FullPath).Replace('\\', '/');
            if ((path.StartsWith("input/", StringComparison.Ordinal) || path.StartsWith("theme/", StringComparison.Ordinal) || path == "versions.json")
                && !path.EndsWith("mermaid.min.js", StringComparison.Ordinal)) timer.Change(800, Timeout.Infinite);
        }
        watcher.Changed += Changed;
        watcher.Created += Changed;
        watcher.Deleted += Changed;
        watcher.Renamed += (sender, args) => Changed(sender, args);
        context.Wyam(new WyamSettings
        {
            EnvironmentVariables = new Dictionary<string, string> { ["DOTNET_ROLL_FORWARD"] = "Major" },
            // Cake.Wyam's Preview option builds first. Serve the already assembled site instead.
            ArgumentCustomization = _ => new ProcessArgumentBuilder()
                .Append("preview").AppendQuoted(Path.Combine(root, "artifacts/docs/preview"))
        });
    }
}
