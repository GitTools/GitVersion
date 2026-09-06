using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Path = System.IO.Path;

namespace Docs.Utilities;

public sealed record DocsEdition(string Id, string Label, string BasePath, string Status, string? Framework = null, string[]? Aliases = null);
public sealed record DocsManifest(DocsEdition[] Versions, int DiscoverFromMajor = 7);
public sealed record DocsPackage(string Id, string Version, string Sha512);
public sealed record PreparedEdition(DocsEdition Edition, string Release, string SourceCommit, string ContentRoot,
    string? AssemblyRoot, DocsPackage[] Packages, string[]? Dependencies = null);

/// <summary>Resolves release metadata once and prepares immutable, cached documentation inputs.</summary>
public sealed class DocsInputs(string repositoryRoot, Action<string> log)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string cache = Path.Combine(repositoryRoot, "artifacts", "docs", "cache");
    private readonly HttpClient http = CreateClient();

    private static bool Matches(string value, string pattern) => Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static DocsManifest ReadManifest(string path)
    {
        var manifest = JsonSerializer.Deserialize<DocsManifest>(File.ReadAllText(path), Json)
            ?? throw new InvalidOperationException("The documentation version manifest is empty.");
        if (manifest.DiscoverFromMajor < 1)
        {
            throw new InvalidOperationException("Release discovery requires a positive major version.");
        }

        var paths = new HashSet<string>(["/docs", "/api", "/assets", "/schemas"], StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var version in manifest.Versions)
        {
            ValidateEdition(version, ids, paths);
        }
        return manifest;
    }

    private static void ValidateEdition(DocsEdition version, HashSet<string> ids, HashSet<string> paths)
    {
        if (!ids.Add(version.Id) || string.IsNullOrWhiteSpace(version.Label))
        {
            throw new InvalidOperationException("Documentation IDs must be unique and labels cannot be empty.");
        }

        if (!Matches(version.Id, @"^\d+\.\d+$") || version.Framework is not null && !Matches(version.Framework, @"^net(?:coreapp)?\d+\.\d+$"))
        {
            throw new InvalidOperationException($"Edition '{version.Id}' must use a major.minor ID and, if specified, a valid framework such as net8.0.");
        }

        if (version.BasePath != "/" + version.Id || version.Label != version.Id || version.Status is not ("release" or "development"))
        {
            throw new InvalidOperationException("Editions must use their numeric ID as label and path, with release or development status.");
        }

        var invalidPath = new[] { version.BasePath }.Concat(version.Aliases ?? [])
            .FirstOrDefault(prefix => !Matches(prefix, @"^/\d+\.\d+(?:\.\d+)?$") || !paths.Add(prefix));
        if (invalidPath is not null)
        {
            throw new InvalidOperationException($"Invalid or duplicate documentation path '{invalidPath}'.");
        }
    }

    public static string LatestPatch(string line, IEnumerable<string> releases) => releases
        .Where(v => Matches(v, @"^\d+\.\d+\.\d+$") && v.StartsWith(line + ".", StringComparison.Ordinal))
        .OrderByDescending(Version.Parse).FirstOrDefault()
        ?? throw new InvalidOperationException($"No stable release found for {line}.");

    public static DocsEdition[] ResolveEditions(DocsManifest manifest, IEnumerable<string> releases)
    {
        var editions = manifest.Versions.ToDictionary(e => e.Id, StringComparer.Ordinal);
        foreach (var version in releases.Where(v => Matches(v, @"^\d+\.\d+\.\d+$")).Select(Version.Parse)
                     .Where(v => v.Major >= manifest.DiscoverFromMajor))
        {
            var id = $"{version.Major}.{version.Minor}";
            editions.TryAdd(id, new(id, id, "/" + id, "release"));
            editions[id] = editions[id] with { Status = "release" };
        }
        if (editions.Count == 0)
        {
            throw new InvalidOperationException("No stable documentation editions available.");
        }

        var result = editions.Values.OrderByDescending(e => Version.Parse(e.Id)).ToArray();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicatePath = result.SelectMany(edition => new[] { edition.BasePath }.Concat(edition.Aliases ?? []))
            .FirstOrDefault(path => !paths.Add(path));
        if (duplicatePath is not null)
        {
            throw new InvalidOperationException($"Duplicate discovered edition path: {duplicatePath}");
        }

        return result;
    }

    public async Task<PreparedEdition[]> Prepare()
    {
        var manifest = ReadManifest(Path.Combine(repositoryRoot, "docs", "versions.json"));
        var releases = await ReadReleases();
        var prepared = new List<PreparedEdition>();
        foreach (var selectedEdition in ResolveEditions(manifest, releases.Keys))
        {
            prepared.Add(await PrepareEdition(selectedEdition, releases));
        }
        var receipt = Path.Combine(repositoryRoot, "artifacts", "docs", "inputs.json");
        Directory.CreateDirectory(Path.GetDirectoryName(receipt)!);
        await File.WriteAllTextAsync(receipt, JsonSerializer.Serialize(prepared, Json));
        return [.. prepared];
    }

    private async Task<Dictionary<string, JsonElement>> ReadReleases()
    {
        var releases = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        // GitHub's stable published releases are authoritative; package feeds alone can be partially published.
        var page = 1;
        while (true)
        {
            using var response = await GetJson($"https://api.github.com/repos/GitTools/GitVersion/releases?per_page=100&page={page}");
            var items = response.RootElement.EnumerateArray().ToArray();
            foreach (var release in items.Where(r => !r.GetProperty("draft").GetBoolean() && !r.GetProperty("prerelease").GetBoolean()))
            {
                var tag = release.GetProperty("tag_name").GetString()!;
                var number = tag.TrimStart('v');
                if (Matches(number, @"^\d+\.\d+\.\d+$"))
                {
                    releases[number] = release.Clone();
                }
            }
            page++;
            if (items.Length < 100)
            {
                break;
            }
        }
        return releases;
    }

    private async Task<PreparedEdition> PrepareEdition(DocsEdition edition, Dictionary<string, JsonElement> releases)
    {
        if (edition.Status == "development")
        {
            var commitId = await ResolveSourceCommit();
            return new(edition, "development", commitId, repositoryRoot, null, []);
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
        var msbuild = await Package("gitversion.msbuild", version, packages);
        var framework = edition.Framework ?? Directory.GetDirectories(Path.Combine(tool, "tools"))
            .Select(Path.GetFileName).Where(n => n is not null && Matches(n, @"^net\d+\.\d+$"))
            .OrderByDescending(n => Version.Parse(n![3..])).FirstOrDefault()
            ?? throw new InvalidOperationException($"No supported tool framework found for {version}.");
        edition = edition with { Framework = framework };
        var bundle = Path.Combine(tool, "tools", edition.Framework!, "any");
        if (!Directory.Exists(bundle))
        {
            throw new InvalidOperationException($"{version} has no {edition.Framework} tool bundle.");
        }
        // A separate staging area preserves the original package content and its checksum.
        var assemblies = Path.Combine(repositoryRoot, "artifacts", "docs", "inputs", edition.Id, "assemblies");
        if (Directory.Exists(assemblies))
        {
            Directory.Delete(assemblies, true);
        }

        CopyTree(bundle, assemblies);
        var msbuildBundle = Path.Combine(msbuild, "tools", edition.Framework!);
        var msbuildAssembly = Path.Combine(msbuildBundle, "GitVersion.MsBuild.dll");
        if (!File.Exists(msbuildAssembly))
        {
            throw new InvalidOperationException($"{version} has no {edition.Framework} MSBuild assembly.");
        }
        // Retain the tool's dependency versions; add the MSBuild assembly and any additional dependencies.
        foreach (var file in Directory.GetFiles(msbuildBundle).Where(p => Path.GetExtension(p) is ".dll" or ".xml"))
        {
            var target = Path.Combine(assemblies, Path.GetFileName(file));
            if (!File.Exists(target))
            {
                File.Copy(file, target);
            }
        }
        var xml = Directory.GetFiles(core, "GitVersionCore.xml", SearchOption.AllDirectories)
            .OrderBy(p => p.Contains(edition.Framework!, StringComparison.Ordinal) ? 0 : 1).FirstOrDefault()
            ?? throw new InvalidOperationException($"GitVersion.Core {version} is missing XML documentation.");
        File.Copy(xml, Path.Combine(assemblies, "GitVersion.Core.xml"), true);
        DocsApi.Prepare(contentRoot, assemblies);
        using var dependencies = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(assemblies, "gitversion.deps.json")));
        return new(edition, version, sha, contentRoot, assemblies, [.. packages],
            [.. dependencies.RootElement.GetProperty("libraries").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)]);
    }

    private async Task<string> ResolveSourceCommit()
    {
        var executable = OperatingSystem.IsWindows() ? "git.exe" : "git";
        var gitPath = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
            .Where(Path.IsPathFullyQualified).Select(p => Path.Combine(p, executable)).FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException("Git was not found on PATH.");
        var start = new ProcessStartInfo(gitPath) { WorkingDirectory = repositoryRoot, RedirectStandardOutput = true };
        start.ArgumentList.Add("rev-parse");
        start.ArgumentList.Add("HEAD");
        using var process = Process.Start(start)!;
        var commitId = (await process.StandardOutput.ReadToEndAsync()).Trim();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("Could not resolve documentation source revision.");
        }

        return commitId;
    }

    private async Task<string> Package(string id, string version, List<DocsPackage> packages)
    {
        var path = await Archive($"https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg", "packages", id + "/" + version);
        packages.Add(new(id, version, await File.ReadAllTextAsync(path + ".sha512")));
        return path;
    }

    private async Task<string> Archive(string url, string category, string identity)
    {
        var directory = Path.Combine(this.cache, category, identity);
        var archive = directory + ".zip";
        var checksum = directory + ".sha512";
        Directory.CreateDirectory(Path.GetDirectoryName(directory)!);
        if (File.Exists(archive) && File.Exists(checksum))
        {
            if (Convert.ToBase64String(SHA512.HashData(await File.ReadAllBytesAsync(archive))) != (await File.ReadAllTextAsync(checksum)).Trim())
            {
                throw new InvalidOperationException($"Cached documentation input failed checksum verification: {identity}");
            }

            log($"Using cached docs input: {identity}");
        }
        else
        {
            log($"Downloading docs input: {identity}");
            var bytes = await this.http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(archive + ".tmp", bytes);
            File.Move(archive + ".tmp", archive, true);
            await File.WriteAllTextAsync(checksum, Convert.ToBase64String(SHA512.HashData(bytes)));
        }
        // Extraction is cheap and avoids trusting a partially extracted or locally changed cache tree.
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }

        await ZipFile.ExtractToDirectoryAsync(archive, directory);
        return directory;
    }

    private async Task<JsonDocument> GetJson(string url)
    {
        var attempt = 0;
        while (true)
        {
            try { return await GetJsonOnce(url); }
            catch (HttpRequestException e) when (attempt < 3 && (e.StatusCode is null || (int)e.StatusCode >= 500 || (int)e.StatusCode == 429))
            {
                log("Documentation metadata request failed temporarily; retrying...");
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt));
                attempt++;
            }
        }
    }

    private async Task<JsonDocument> GetJsonOnce(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? Environment.GetEnvironmentVariable("GH_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await this.http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GitVersion-Documentation/1.0");
        return client;
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
