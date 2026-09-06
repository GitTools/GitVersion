using System.Diagnostics;
using Path = System.IO.Path;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Docs.Utilities;

public sealed record DocsEdition(string Id, string Label, string BasePath, string Status, string? Framework = null, string[]? Aliases = null);
public sealed record DocsManifest(DocsEdition[] Versions);
public sealed record DocsPackage(string Id, string Version, string Sha512);
public sealed record PreparedEdition(DocsEdition Edition, string Release, string SourceCommit, string ContentRoot,
    string? AssemblyRoot, DocsPackage[] Packages, string[]? Dependencies = null);

/// <summary>Resolves release metadata once and prepares immutable, cached documentation inputs.</summary>
public sealed class DocsInputs(string repositoryRoot, Action<string> log)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string cache = Path.Combine(repositoryRoot, "artifacts", "docs", "cache");
    private readonly HttpClient http = CreateClient();

    public static DocsManifest ReadManifest(string path)
    {
        var manifest = JsonSerializer.Deserialize<DocsManifest>(File.ReadAllText(path), Json)
            ?? throw new InvalidOperationException("The documentation version manifest is empty.");
        if (manifest.Versions.Length == 0 || manifest.Versions.Count(v => v.Id == "current") != 1)
            throw new InvalidOperationException("The manifest must contain exactly one current edition.");
        var paths = new HashSet<string>(["/docs", "/api", "/assets", "/schemas"], StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var version in manifest.Versions)
        {
            if (!ids.Add(version.Id) || string.IsNullOrWhiteSpace(version.Label))
                throw new InvalidOperationException("Documentation IDs must be unique and labels cannot be empty.");
            if (version.Id != "current" && (!Regex.IsMatch(version.Id, @"^\d+\.\d+$") || version.Framework is null || !Regex.IsMatch(version.Framework, @"^net(?:coreapp)?\d+\.\d+$")))
                throw new InvalidOperationException($"Edition '{version.Id}' must specify major.minor and a framework.");
            if ((version.Id == "current") != (version.BasePath == ""))
                throw new InvalidOperationException("Only the current edition owns the site root.");
            foreach (var prefix in new[] { version.BasePath }.Concat(version.Aliases ?? []))
            {
                if ((prefix != "" && !Regex.IsMatch(prefix, @"^/[a-zA-Z0-9.-]+$")) || prefix.Contains("..", StringComparison.Ordinal) || !paths.Add(prefix))
                    throw new InvalidOperationException($"Invalid or duplicate documentation path '{prefix}'.");
            }
        }
        return manifest;
    }

    public static string LatestPatch(string line, IEnumerable<string> releases) => releases
        .Where(v => Regex.IsMatch(v, @"^\d+\.\d+\.\d+$") && v.StartsWith(line + ".", StringComparison.Ordinal))
        .OrderByDescending(Version.Parse).FirstOrDefault()
        ?? throw new InvalidOperationException($"No stable release found for {line}.");

    public async Task<PreparedEdition[]> Prepare()
    {
        var manifest = ReadManifest(Path.Combine(repositoryRoot, "docs", "versions.json"));
        var releases = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        // GitHub's stable published releases are authoritative; package feeds alone can be partially published.
        for (var page = 1; ; page++)
        {
            using var response = await GetJson($"https://api.github.com/repos/GitTools/GitVersion/releases?per_page=100&page={page}");
            var items = response.RootElement.EnumerateArray().ToArray();
            foreach (var release in items.Where(r => !r.GetProperty("draft").GetBoolean() && !r.GetProperty("prerelease").GetBoolean()))
            {
                var tag = release.GetProperty("tag_name").GetString()!;
                var number = tag.TrimStart('v');
                if (Regex.IsMatch(number, @"^\d+\.\d+\.\d+$")) releases[number] = release.Clone();
            }
            if (items.Length < 100) break;
        }
        var prepared = new List<PreparedEdition>();
        foreach (var edition in manifest.Versions)
        {
            if (edition.Id == "current")
            {
                prepared.Add(new(edition, "development", Git("rev-parse", "HEAD"), repositoryRoot, null, []));
                continue;
            }
            var version = LatestPatch(edition.Id, releases.Keys);
            var tag = releases[version].GetProperty("tag_name").GetString()!;
            using var commit = await GetJson($"https://api.github.com/repos/GitTools/GitVersion/commits/{Uri.EscapeDataString(tag)}");
            var sha = commit.RootElement.GetProperty("sha").GetString()!;
            log($"Documentation {edition.Label}: resolved {version} ({sha[..12]})");
            var source = await Archive($"https://codeload.github.com/GitTools/GitVersion/zip/{sha}", "source", sha);
            var contentRoot = Directory.GetDirectories(source).Single();
            var packages = new List<DocsPackage>();
            var tool = await Package("gitversion.tool", version, packages);
            var core = await Package("gitversion.core", version, packages);
            var bundle = Path.Combine(tool, "tools", edition.Framework!, "any");
            if (!Directory.Exists(bundle)) throw new InvalidOperationException($"{version} has no {edition.Framework} tool bundle.");
            // A separate staging area preserves the original package content and its checksum.
            var assemblies = Path.Combine(repositoryRoot, "artifacts", "docs", "inputs", edition.Id, "assemblies");
            if (Directory.Exists(assemblies)) Directory.Delete(assemblies, true);
            CopyTree(bundle, assemblies);
            var xml = Directory.GetFiles(core, "GitVersionCore.xml", SearchOption.AllDirectories)
                .OrderBy(p => p.Contains(edition.Framework!, StringComparison.Ordinal) ? 0 : 1).FirstOrDefault()
                ?? throw new InvalidOperationException($"GitVersion.Core {version} is missing XML documentation.");
            File.Copy(xml, Path.Combine(assemblies, "GitVersion.Core.xml"), true);
            DocsApi.Prepare(contentRoot, assemblies);
            using var dependencies = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(assemblies, "gitversion.deps.json")));
            prepared.Add(new(edition, version, sha, contentRoot, assemblies, [.. packages],
                dependencies.RootElement.GetProperty("libraries").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray()));
        }
        var receipt = Path.Combine(repositoryRoot, "artifacts", "docs", "inputs.json");
        Directory.CreateDirectory(Path.GetDirectoryName(receipt)!);
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(prepared, Json));
        return [.. prepared];
    }

    private async Task<string> Package(string id, string version, List<DocsPackage> packages)
    {
        var path = await Archive($"https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg", "packages", id + "/" + version);
        packages.Add(new(id, version, await File.ReadAllTextAsync(path + ".sha512")));
        return path;
    }

    private async Task<string> Archive(string url, string category, string identity)
    {
        var directory = Path.Combine(cache, category, identity);
        var archive = directory + ".zip";
        var checksum = directory + ".sha512";
        Directory.CreateDirectory(Path.GetDirectoryName(directory)!);
        if (File.Exists(archive) && File.Exists(checksum))
        {
            if (Convert.ToBase64String(SHA512.HashData(await File.ReadAllBytesAsync(archive))) != (await File.ReadAllTextAsync(checksum)).Trim())
                throw new InvalidOperationException($"Cached documentation input failed checksum verification: {identity}");
            log($"Using cached docs input: {identity}");
        }
        else
        {
            log($"Downloading docs input: {identity}");
            var bytes = await http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(archive + ".tmp", bytes);
            File.Move(archive + ".tmp", archive, true);
            await File.WriteAllTextAsync(checksum, Convert.ToBase64String(SHA512.HashData(bytes)));
        }
        // Extraction is cheap and avoids trusting a partially extracted or locally changed cache tree.
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
        ZipFile.ExtractToDirectory(archive, directory);
        return directory;
    }

    private async Task<JsonDocument> GetJson(string url)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { return await GetJsonOnce(url); }
            catch (HttpRequestException e) when (attempt < 3 && (e.StatusCode is null || (int)e.StatusCode >= 500 || (int)e.StatusCode == 429))
            {
                log("Documentation metadata request failed temporarily; retrying...");
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt));
            }
        }
    }

    private async Task<JsonDocument> GetJsonOnce(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? Environment.GetEnvironmentVariable("GH_TOKEN");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GitVersion-Documentation/1.0");
        return client;
    }

    private string Git(params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = repositoryRoot, RedirectStandardOutput = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException("Could not resolve documentation source revision.");
        return output.Trim();
    }

    public static void CopyTree(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}
