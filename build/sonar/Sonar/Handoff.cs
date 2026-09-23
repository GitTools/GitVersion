using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

namespace GitVersion.Sonar;

public sealed record FileDigest(long Bytes, string Sha256);
public sealed record Manifest(int Schema, string Scanner, RunIdentity Identity, string Producer,
    SortedDictionary<string, FileDigest> Files, SortedDictionary<string, FileDigest> Configuration);
public sealed record AnalysisScope(int Projects, int Reports, int Sources, int CoveredSources);

public static class Handoff
{
    public const string ScannerVersion = "11.3.0";
    private static readonly XNamespace Ns = "http://www.sonarsource.com/msbuild/integration/2015/1"; // NOSONAR XML namespace identifier, never a network request.
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
    private static readonly HashSet<string> DataExtensions = [".xml", ".json", ".pb", ".txt", ".ucfgs", ".typedefs", ".udg", ".log", ".lock"];
    private const string ScannerDirectory = ".sonarqube";
    private static readonly string[] Properties = ["sonar.cs.roslyn.reportFilePaths", "sonar.cs.analyzer.projectOutPaths", "sonar.cs.scanner.telemetry"];

    public static T Read<T>(string path)
    {
        SafeFiles.Require(new FileInfo(path).Length <= SafeFiles.MaxFileBytes, "Oversized JSON");
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) ?? throw new InvalidDataException("Missing JSON document");
    }

    public static void Write<T>(string path, T value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json));

    public static SortedDictionary<string, FileDigest> Inventory(string root)
    {
        var result = new SortedDictionary<string, FileDigest>(StringComparer.Ordinal);
        foreach (var path in SafeFiles.Files(root))
        {
            using var stream = File.OpenRead(path);
            result.Add(Path.GetRelativePath(root, path).Replace('\\', '/'), new(stream.Length, Convert.ToHexStringLower(SHA256.HashData(stream))));
        }
        return result;
    }

    public static SortedDictionary<string, FileDigest> Configuration(string scanner)
    {
        var conf = Path.Combine(scanner, "conf");
        var result = new SortedDictionary<string, FileDigest>(StringComparer.Ordinal);
        // Rules and analyzer identities must agree with fresh trusted preparation. No config is imported.
        foreach (var path in SafeFiles.Files(conf).Where(p => p.EndsWith(".ruleset", StringComparison.Ordinal) || Path.GetFileName(p) == "SonarLint.xml"))
        {
            using var stream = File.OpenRead(path);
            result.Add(Path.GetRelativePath(conf, path), new(stream.Length, Convert.ToHexStringLower(SHA256.HashData(stream))));
        }
        var configuration = SafeFiles.ReadXml(Path.Combine(conf, "SonarQubeAnalysisConfig.xml"));
        var plugins = configuration.Descendants(Ns + "AnalyzerPlugin").ToArray();
        SafeFiles.Require(plugins.Length > 0, "Missing analyzer identities");
        foreach (var plugin in plugins)
        {
            var key = plugin.Attribute("Key")?.Value ?? throw new InvalidDataException("Missing analyzer key");
            var version = plugin.Attribute("Version")?.Value ?? throw new InvalidDataException("Missing analyzer version");
            foreach (var assembly in plugin.Descendants(Ns + "Path").Select(e => e.Value).Where(p => p.EndsWith(".dll", StringComparison.Ordinal)))
            {
                SafeFiles.Require(new FileInfo(assembly).Length <= SafeFiles.MaxFileBytes && new FileInfo(assembly).LinkTarget is null, "Invalid prepared analyzer");
                using var stream = File.OpenRead(assembly);
                result.Add($"analyzers/{key}/{version}/{Path.GetFileName(assembly)}", new(stream.Length, Convert.ToHexStringLower(SHA256.HashData(stream))));
            }
        }
        SafeFiles.Require(result.Count > 0, "Missing analyzer configuration");
        return result;
    }

    public static void Collect(string repository, string scanner, string coverage, string bundle, RunIdentity identity)
    {
        identity.Validate();
        SafeFiles.Require(!Directory.Exists(bundle), "Bundle already exists");
        var payload = Path.Combine(bundle, "payload");
        foreach (var path in SafeFiles.Files(Path.Combine(scanner, "out")))
        {
            Copy(path, Path.Combine(payload, "sonar/out", Path.GetRelativePath(Path.Combine(scanner, "out"), path)));
        }

        foreach (var path in SafeFiles.Files(Path.Combine(scanner, "conf")).Where(p => Path.GetFileName(p) == "FilesToAnalyze.txt"))
        {
            Copy(path, Path.Combine(payload, "sonar/conf", Path.GetRelativePath(Path.Combine(scanner, "conf"), path)));
        }

        foreach (var path in SafeFiles.Files(coverage).Where(p => p.EndsWith(".xml", StringComparison.Ordinal) && Path.GetFileName(p).Contains("cobertura", StringComparison.Ordinal)))
        {
            Copy(path, Path.Combine(payload, "coverage", Path.GetRelativePath(coverage, path)));
        }

        var manifest = new Manifest(2, ScannerVersion, identity, Path.GetFullPath(repository), Inventory(payload), Configuration(scanner));
        Write(Path.Combine(bundle, "manifest.json"), manifest);
        Verify(repository, bundle, identity);
    }

    public static AnalysisScope Verify(string repository, string bundle, RunIdentity expected)
    {
        var manifest = Read<Manifest>(SafeFiles.Resolve(bundle, "manifest.json"));
        SafeFiles.Require(manifest.Schema == 2 && manifest.Scanner == ScannerVersion, "Unsupported handoff version");
        manifest.Identity.EnsureCurrent(expected);
        SafeFiles.Require(Path.IsPathFullyQualified(manifest.Producer) && Path.GetFullPath(manifest.Producer) == manifest.Producer, "Invalid producer root");
        var payload = SafeFiles.Resolve(bundle, "payload");
        SafeFiles.Require(Equal(manifest.Files, Inventory(payload)), "Artifact inventory mismatch");
        foreach (var path in manifest.Files.Keys)
        {
            SafeFiles.Relative(path);
            SafeFiles.Require(DataExtensions.Contains(Path.GetExtension(path)), "Unexpected analysis file type");
            SafeFiles.Require(path.StartsWith("sonar/out/", StringComparison.Ordinal) ||
                              path.StartsWith("coverage/", StringComparison.Ordinal) && path.EndsWith(".xml", StringComparison.Ordinal) ||
                              path.StartsWith("sonar/conf/", StringComparison.Ordinal) && Path.GetFileName(path) == "FilesToAnalyze.txt", "Unexpected handoff path");
        }
        return Inspect(repository, payload, manifest.Producer, null);
    }

    public static void Import(string repository, string bundle, RunIdentity expected)
    {
        Verify(repository, bundle, expected);
        var manifest = Read<Manifest>(Path.Combine(bundle, "manifest.json"));
        // Opaque analyzer formats contain absolute paths. Until a fully audited remapper exists,
        // require the identical workspace used by GitHub's Ubuntu producer.
        SafeFiles.Require(Path.GetFullPath(repository) == manifest.Producer, "Producer/publisher workspace differs; rebuild on the supported runner");
        SafeFiles.Require(Equal(manifest.Configuration, Configuration(Path.Combine(repository, ScannerDirectory))), "Analyzer configuration changed; rerun CI");
        Inspect(repository, Path.Combine(bundle, "payload"), manifest.Producer, Path.Combine(repository, ScannerDirectory));
    }

    private static (HashSet<string> Projects, HashSet<string> Sources) InspectProjects(string repository, string payload, string producer, string? destination)
    {
        var projects = new HashSet<string>(StringComparer.Ordinal);
        var sources = new HashSet<string>(StringComparer.Ordinal);
        var ids = new HashSet<Guid>();
        var outRoot = Path.Combine(payload, "sonar/out");
        var metadata = Directory.GetFiles(outRoot, "ProjectInfo.xml", SearchOption.AllDirectories);
        SafeFiles.Require(metadata.Length > 0, "Missing project metadata");
        foreach (var path in metadata)
        {
            var folder = Path.GetFileName(Path.GetDirectoryName(path))!;
            SafeFiles.Require(folder.All(char.IsAsciiDigit) && Path.GetDirectoryName(Path.GetDirectoryName(path)) == outRoot, "Invalid project folder");
            var info = SafeFiles.ReadXml(path).Root ?? throw new InvalidDataException("Missing project XML");
            string Value(string name) => info.Element(Ns + name)?.Value ?? throw new InvalidDataException("Missing project field: " + name);
            SafeFiles.Require(info.Name == Ns + "ProjectInfo" && Value("ProjectLanguage") == "C#", "Unsupported project metadata");
            SafeFiles.Require(Value("IsExcluded") == "false", "Excluded project would reduce analysis scope");
            var project = Owned(Value("FullPath"), producer, repository);
            SafeFiles.Require(project.EndsWith(".csproj", StringComparison.Ordinal) && projects.Add(project), "Duplicate or invalid project");
            var id = Guid.Parse(Value("ProjectGuid"));
            SafeFiles.Require(id == ProjectIds.Identifier(project) && ids.Add(id), "Invalid or duplicate project ID");
            var type = project.Contains(".Tests/", StringComparison.Ordinal) || project.Contains("/GitVersion.Testing/", StringComparison.Ordinal) ? "Test" : "Product";
            SafeFiles.Require(Value("ProjectType") == type, "Unexpected test classification");
            var values = new[] { $"{producer}/.sonarqube/out/{folder}/Issues.json", $"{producer}/.sonarqube/out/{folder}", $"{producer}/.sonarqube/out/{folder}/Telemetry.json" };
            ValidateSettings(info, values);
            var accepted = ReadSources(repository, payload, producer, folder, sources);
            if (destination is null)
            {
                continue;
            }

            var target = SafeFiles.Resolve(destination, "out/" + folder);
            foreach (var file in SafeFiles.Files(Path.GetDirectoryName(path)!).Where(file => file != path))
            {
                Copy(file, SafeFiles.Resolve(target, Path.GetRelativePath(Path.GetDirectoryName(path)!, file)));
            }

            var list = SafeFiles.Resolve(destination, $"conf/{folder}/FilesToAnalyze.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(list)!);
            File.WriteAllLines(list, accepted);
            // Construct a new document. No arbitrary fields, settings, executable paths or config survive.
            var clean = new XElement(Ns + "ProjectInfo",
                new XElement(Ns + "ProjectName", Path.GetFileNameWithoutExtension(project)), new XElement(Ns + "ProjectLanguage", "C#"),
                new XElement(Ns + "ProjectType", type), new XElement(Ns + "ProjectGuid", id), new XElement(Ns + "FullPath", Path.Combine(repository, project)),
                new XElement(Ns + "IsExcluded", false), new XElement(Ns + "AnalysisResultFiles", new XElement(Ns + "AnalysisResultFile", new XAttribute("Id", "FilesToAnalyze"), new XAttribute("Location", list))),
                new XElement(Ns + "AnalysisSettings", Properties.Select((p, i) => new XElement(Ns + "Property", new XAttribute("Name", p), values[i]))),
                new XElement(Ns + "Configuration", "Release"), new XElement(Ns + "Platform", "AnyCPU"), new XElement(Ns + "TargetFramework", "net10.0"));
            new XDocument(clean).Save(Path.Combine(target, "ProjectInfo.xml"));
        }
        var expectedProjects = ExpectedProjects(repository);
        SafeFiles.Require(projects.SetEquals(expectedProjects), "Project inventory differs from the three solutions");
        SafeFiles.Require(sources.Any(p => p.EndsWith(".cs", StringComparison.Ordinal)), "Missing analyzed C# sources");
        return (projects, sources);
    }

    private static void ValidateSettings(XElement info, string[] values)
    {
        var settings = info.Element(Ns + "AnalysisSettings")?.Elements().ToArray() ?? [];
        SafeFiles.Require(settings.Length == 3 && settings.Select(e => e.Attribute("Name")?.Value).ToHashSet().SetEquals(Properties), "Unexpected scanner properties");
        for (var i = 0; i < Properties.Length; i++)
        {
            SafeFiles.Require(settings.Single(e => e.Attribute("Name")?.Value == Properties[i]).Value == values[i], "Scanner property redirects outside project output");
        }
    }

    private static HashSet<string> ExpectedProjects(string repository)
    {
        var expectedProjects = new HashSet<string>(StringComparer.Ordinal);
        foreach (var solution in new[] { "src/GitVersion.slnx", "new-cli/GitVersion.slnx", "build/CI.slnx" })
        {
            var full = SafeFiles.Resolve(repository, solution);
            foreach (var entry in SafeFiles.ReadXml(full).Descendants("Project"))
            {
                var relative = entry.Attribute("Path")?.Value ?? throw new InvalidDataException("Missing solution project path");
                var path = SafeFiles.Resolve(Path.GetDirectoryName(full)!, relative);
                expectedProjects.Add(Path.GetRelativePath(repository, path).Replace('\\', '/'));
            }
        }
        return expectedProjects;
    }

    private static List<string> ReadSources(string repository, string payload, string producer, string folder, HashSet<string> sources)
    {
        var sourceList = SafeFiles.Resolve(payload, $"sonar/conf/{folder}/FilesToAnalyze.txt");
        var accepted = new List<string>();
        foreach (var source in File.ReadAllLines(sourceList))
        {
            SafeFiles.Require(!source.Split('/').Contains(".."), "Traversal in source list");
            if (!source.StartsWith(producer + "/", StringComparison.Ordinal))
            {
                continue;
            }

            var relative = SafeFiles.Relative(source[(producer.Length + 1)..]);
            SafeFiles.Require(!relative.Split('/').Any(p => p is ".git" or ".sonarqube"), "Reserved scanner/credential source path");
            if (relative.Split('/').Any(p => p is "obj" or "bin") || !File.Exists(SafeFiles.Resolve(repository, relative)))
            {
                continue;
            }

            sources.Add(relative);
            accepted.Add(Path.Combine(repository, relative));
        }
        return accepted;
    }

    private static AnalysisScope Inspect(string repository, string payload, string producer, string? destination)
    {
        var (projects, sources) = InspectProjects(repository, payload, producer, destination);
        var expectedReports = ProjectIds.Projects(repository).Where(p => p.StartsWith(Path.Combine(repository, "src") + Path.DirectorySeparatorChar, StringComparison.Ordinal) && p.EndsWith(".Tests.csproj", StringComparison.Ordinal)).Select(p => Path.GetFileNameWithoutExtension(p)).Append("Sonar.Tests").ToHashSet();
        var actual = new HashSet<string>();
        var covered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var report in SafeFiles.Files(Path.Combine(payload, "coverage")))
        {
            var relative = Path.GetRelativePath(Path.Combine(payload, "coverage"), report).Replace('\\', '/');
            var parts = relative.Split('/');
            SafeFiles.Require(parts.Length == 3 && parts[1] == "net10.0" && parts[2].Contains("cobertura", StringComparison.Ordinal) && actual.Add(parts[0]), "Invalid or duplicate coverage report");
            var doc = SafeFiles.ReadXml(report);
            SafeFiles.Require(doc.Root?.Name == "coverage" && doc.Descendants("line").Any(), "Missing Cobertura line data");
            var roots = doc.Descendants("source").Select(e => e.Value).ToArray();
            var mapped = 0;
            foreach (var item in doc.Descendants("class").ToArray())
            {
                var filename = item.Attribute("filename")?.Value ?? throw new InvalidDataException("Missing coverage filename");
                var candidates = roots.Select(r => r.TrimEnd('/') + "/" + filename.TrimStart('/')).Append(filename);
                var match = candidates.FirstOrDefault(p => p.StartsWith(producer + "/", StringComparison.Ordinal));
                if (match is null) { item.Remove(); continue; }
                var source = Owned(match, producer, repository);
                SafeFiles.Require(sources.Contains(source), "Coverage file absent from analysis");
                item.SetAttributeValue("filename", Path.Combine(repository, source));
                covered.Add(source);
                mapped++;
            }
            SafeFiles.Require(mapped > 0, "Coverage report contains no repository sources");
            if (destination is not null)
            {
                doc.Root!.Element("sources")?.ReplaceWith(new XElement("sources", new XElement("source", "/")));
                var output = SafeFiles.Resolve(destination, $"coverage/{parts[0]}.xml");
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                doc.Save(output);
            }
        }
        SafeFiles.Require(actual.SetEquals(expectedReports), "Incomplete canonical coverage inventory");
        return new(projects.Count, actual.Count, sources.Count, covered.Count);
    }

    private static bool Equal(IReadOnlyDictionary<string, FileDigest> left, IReadOnlyDictionary<string, FileDigest> right) =>
        left.Count == right.Count && left.All(p => right.TryGetValue(p.Key, out var value) && value == p.Value);

    private static string Owned(string path, string producer, string repository)
    {
        SafeFiles.Require(path.StartsWith(producer + "/", StringComparison.Ordinal), "Source outside producer checkout");
        var relative = SafeFiles.Relative(path[(producer.Length + 1)..]);
        SafeFiles.Require(!relative.Split('/').Any(p => p is ".git" or ".sonarqube"), "Reserved scanner/credential source path");
        SafeFiles.Require(File.Exists(SafeFiles.Resolve(repository, relative)), "Missing repository source");
        return relative;
    }

    private static void Copy(string source, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, false);
    }
}
