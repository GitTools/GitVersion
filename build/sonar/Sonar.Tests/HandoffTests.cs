using System.Xml.Linq;

namespace GitVersion.Sonar.Tests;

[TestFixture]
public class HandoffTests
{
    private static readonly XNamespace Ns = "http://www.sonarsource.com/msbuild/integration/2015/1";
    private string root = null!;
    private string repository = null!;
    private string scanner = null!;
    private string coverage = null!;
    private string bundle = null!;
    private static RunIdentity Identity => new("GitTools/GitVersion", 123, 1, "pull_request", new('a', 40), new('b', 40), new('c', 40), 42, "feature", "main");

    [SetUp]
    public void SetUp()
    {
        this.root = Directory.CreateTempSubdirectory("sonar-handoff-tests-").FullName;
        this.repository = Path.Combine(this.root, "repository");
        this.scanner = Path.Combine(this.repository, ".sonarqube");
        this.coverage = Path.Combine(this.root, "coverage");
        this.bundle = Path.Combine(this.root, "bundle");
        var projects = new[] { "src/Foo/Foo.csproj", "src/Foo.Tests/Foo.Tests.csproj", "new-cli/Bar/Bar.csproj", "build/Tool/Tool.csproj", "build/sonar/Sonar.Tests/Sonar.Tests.csproj" };
        for (var i = 0; i < projects.Length; i++) AddProject(projects[i], i);
        Write("src/GitVersion.slnx", "<Solution><Project Path=\"Foo/Foo.csproj\"/><Project Path=\"Foo.Tests/Foo.Tests.csproj\"/></Solution>");
        Write("new-cli/GitVersion.slnx", "<Solution><Project Path=\"Bar/Bar.csproj\"/></Solution>");
        Write("build/CI.slnx", "<Solution><Project Path=\"Tool/Tool.csproj\"/><Project Path=\"sonar/Sonar.Tests/Sonar.Tests.csproj\"/></Solution>");
        ConfigureAnalyzer();
        AddCoverage("Foo.Tests", "src/Foo/Code.cs");
        AddCoverage("Sonar.Tests", "build/Tool/Code.cs");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(this.root, true);

    [Test]
    public void CollectAndVerifyPreserveCompleteProjectAndCoverageScope()
    {
        Collect();
        Assert.That(Handoff.Verify(this.repository, this.bundle, Identity), Is.EqualTo(new AnalysisScope(5, 2, 5, 2)));
        var manifest = Handoff.Read<Manifest>(Path.Combine(this.bundle, "manifest.json"));
        Assert.That(manifest.Identity, Is.EqualTo(Identity));
        Assert.That(manifest.Configuration.Keys, Is.EquivalentTo(new[] { "cs/SonarLint.xml", "analyzers/csharp/1.0/fake.dll" }));
    }

    [Test]
    public void CollectAndVerifyAcceptMixedCaseInventoryNames()
    {
        Write(".sonarqube/out/0/analysis.json", "{}");
        Collect();
        Assert.That(Handoff.Verify(this.repository, this.bundle, Identity), Is.EqualTo(new AnalysisScope(5, 2, 5, 2)));
    }

    [Test]
    public void VerifyRejectsPayloadTampering()
    {
        Collect();
        File.AppendAllText(Path.Combine(this.bundle, "payload/sonar/out/0/Issues.json"), "tampered");
        Assert.That(() => Handoff.Verify(this.repository, this.bundle, Identity), Throws.TypeOf<InvalidDataException>().With.Message.Contains("inventory"));
    }

    [Test]
    public void VerifyRejectsForgedRunIdentity()
    {
        Collect();
        var path = Path.Combine(this.bundle, "manifest.json");
        var manifest = Handoff.Read<Manifest>(path);
        Handoff.Write(path, manifest with { Identity = Identity with { RunId = 456 } });
        Assert.That(() => Handoff.Verify(this.repository, this.bundle, Identity), Throws.TypeOf<InvalidDataException>().With.Message.Contains("mismatched"));
    }

    [TestCase("sonar.scanner.javaExePath", "attacker")]
    [TestCase("sonar.cs.roslyn.reportFilePaths", "/outside/Issues.json")]
    public void CollectRejectsUnsafeMetadataProperties(string name, string value)
    {
        var path = Path.Combine(this.scanner, "out/0/ProjectInfo.xml");
        var document = XDocument.Load(path);
        var property = document.Descendants(Ns + "Property").First();
        property.SetAttributeValue("Name", name);
        property.Value = value;
        document.Save(path);
        Assert.That(Collect, Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void CollectRejectsMissingCoverageReport()
    {
        Directory.Delete(Path.Combine(this.coverage, "Sonar.Tests"), true);
        Assert.That(Collect, Throws.TypeOf<InvalidDataException>().With.Message.Contains("coverage inventory"));
    }

    [Test]
    public void CollectRejectsMissingProjectMetadata()
    {
        Directory.Delete(Path.Combine(this.scanner, "out/2"), true);
        Assert.That(Collect, Throws.TypeOf<InvalidDataException>().With.Message.Contains("Project inventory"));
    }

    [Test]
    public void ImportRejectsAnalyzerConfigurationDriftBeforeWritingOutput()
    {
        Collect();
        PrepareTrustedScanner();
        Write(".sonarqube/conf/cs/SonarLint.xml", "<ChangedRules/>");
        Assert.That(() => Handoff.Import(this.repository, this.bundle, Identity), Throws.TypeOf<InvalidDataException>().With.Message.Contains("configuration changed"));
        Assert.That(Directory.Exists(Path.Combine(this.scanner, "out")), Is.False);
    }

    [Test]
    public void ImportRemovesExternalCoverageAndReconstructsProjectMetadata()
    {
        var metadata = Path.Combine(this.scanner, "out/0/ProjectInfo.xml");
        var original = XDocument.Load(metadata);
        original.Root!.Add(new XElement(Ns + "UntrustedExecutable", "attacker"));
        original.Save(metadata);
        Collect();
        PrepareTrustedScanner();
        Handoff.Import(this.repository, this.bundle, Identity);
        var imported = XDocument.Load(Path.Combine(this.scanner, "coverage/Foo.Tests.xml"));
        Assert.That(imported.Descendants("class").Select(e => e.Attribute("filename")!.Value), Is.EqualTo(new[] { Path.Combine(this.repository, "src/Foo/Code.cs") }));
        Assert.That(imported.Descendants("line").Single().Attribute("hits")!.Value, Is.EqualTo("3"));
        var clean = XDocument.Load(metadata);
        Assert.That(clean.Descendants(Ns + "UntrustedExecutable"), Is.Empty);
        Assert.That(clean.Root!.Element(Ns + "ProjectGuid")!.Value, Is.EqualTo(ProjectIds.Identifier("src/Foo/Foo.csproj").ToString()));
        Assert.That(File.ReadAllLines(Path.Combine(this.scanner, "conf/0/FilesToAnalyze.txt")), Is.EqualTo(new[] { Path.Combine(this.repository, "src/Foo/Code.cs") }));
    }

    [TestCase(1, Handoff.ScannerVersion)]
    [TestCase(2, "untrusted-scanner")]
    public void VerifyRejectsUnsupportedManifestVersion(int schema, string scannerVersion)
    {
        Collect();
        var path = Path.Combine(this.bundle, "manifest.json");
        var manifest = Handoff.Read<Manifest>(path);
        Handoff.Write(path, manifest with { Schema = schema, Scanner = scannerVersion });
        Assert.That(() => Handoff.Verify(this.repository, this.bundle, Identity), Throws.TypeOf<InvalidDataException>().With.Message.Contains("version"));
    }

    [Test]
    public void VerifyRejectsExecutableEvenWithRecomputedInventory()
    {
        Collect();
        var payload = Path.Combine(this.bundle, "payload");
        File.WriteAllText(Path.Combine(payload, "sonar/out/0/attacker.dll"), "untrusted executable");
        var path = Path.Combine(this.bundle, "manifest.json");
        var manifest = Handoff.Read<Manifest>(path);
        Handoff.Write(path, manifest with { Files = Handoff.Inventory(payload) });
        Assert.That(() => Handoff.Verify(this.repository, this.bundle, Identity), Throws.TypeOf<InvalidDataException>().With.Message.Contains("file type"));
    }

    [Test]
    public void CollectRejectsTraversalInSourceList()
    {
        Write(".sonarqube/conf/0/FilesToAnalyze.txt", this.repository + "/src/../outside.cs");
        Assert.That(Collect, Throws.TypeOf<InvalidDataException>().With.Message.Contains("Traversal"));
    }

    [Test]
    public void CollectRejectsCoveredSourceAbsentFromAnalysis()
    {
        Write(".sonarqube/conf/0/FilesToAnalyze.txt", "/outside/dependency.cs");
        Assert.That(Collect, Throws.TypeOf<InvalidDataException>().With.Message.Contains("absent from analysis"));
    }

    [TestCase(".sonarqube/conf/SonarQubeAnalysisConfig.xml")]
    [TestCase(".git/config")]
    public void CollectRejectsCredentialFilesInSourceList(string relative)
    {
        if (relative.EndsWith(".xml", StringComparison.Ordinal))
        {
            var config = XDocument.Load(Path.Combine(this.repository, relative));
            config.Root!.Add(new XElement(Ns + "Token", "credential-canary"));
            config.Save(Path.Combine(this.repository, relative));
        }
        else Write(relative, "credential-canary");
        Write(".sonarqube/conf/0/FilesToAnalyze.txt", Path.Combine(this.repository, "src/Foo/Code.cs") + "\n" + Path.Combine(this.repository, relative));
        Assert.That(Collect, Throws.TypeOf<InvalidDataException>().With.Message.Contains("Reserved"));
    }

    private void Collect() => Handoff.Collect(this.repository, this.scanner, this.coverage, this.bundle, Identity);

    private void PrepareTrustedScanner()
    {
        Directory.Delete(this.scanner, true);
        ConfigureAnalyzer();
    }

    private void ConfigureAnalyzer(string version = "1.0")
    {
        var analyzer = Path.Combine(this.root, "analyzers/fake.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(analyzer)!);
        File.WriteAllText(analyzer, "stable analyzer bytes");
        Write(".sonarqube/conf/cs/SonarLint.xml", "<AnalysisInput/>");
        var configuration = new XElement(Ns + "SonarQubeAnalysisConfig", new XElement(Ns + "AnalyzerPlugin",
            new XAttribute("Key", "csharp"), new XAttribute("Version", version), new XElement(Ns + "Path", analyzer)));
        Write(".sonarqube/conf/SonarQubeAnalysisConfig.xml", configuration.ToString());
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ImportRejectsPreparedAnalyzerBytesOrVersionDrift(bool changeBytes)
    {
        Collect();
        PrepareTrustedScanner();
        if (changeBytes) File.WriteAllText(Path.Combine(this.root, "analyzers/fake.dll"), "changed analyzer bytes");
        else ConfigureAnalyzer("2.0");
        Assert.That(() => Handoff.Import(this.repository, this.bundle, Identity), Throws.TypeOf<InvalidDataException>().With.Message.Contains("configuration changed"));
        Assert.That(Directory.Exists(Path.Combine(this.scanner, "out")), Is.False);
    }

    private void AddProject(string relative, int index)
    {
        Write(relative, "<Project/>");
        var source = Path.GetDirectoryName(relative)!.Replace('\\', '/') + "/Code.cs";
        Write(source, "namespace Sample; public class Code { }");
        var output = $"{this.scanner}/out/{index}";
        var info = new XElement(Ns + "ProjectInfo",
            new XElement(Ns + "ProjectLanguage", "C#"), new XElement(Ns + "IsExcluded", "false"),
            new XElement(Ns + "FullPath", Path.Combine(this.repository, relative)),
            new XElement(Ns + "ProjectGuid", ProjectIds.Identifier(relative)),
            new XElement(Ns + "ProjectType", relative.Contains(".Tests/") ? "Test" : "Product"),
            new XElement(Ns + "AnalysisSettings",
                new XElement(Ns + "Property", new XAttribute("Name", "sonar.cs.roslyn.reportFilePaths"), output + "/Issues.json"),
                new XElement(Ns + "Property", new XAttribute("Name", "sonar.cs.analyzer.projectOutPaths"), output),
                new XElement(Ns + "Property", new XAttribute("Name", "sonar.cs.scanner.telemetry"), output + "/Telemetry.json")));
        Write($".sonarqube/out/{index}/ProjectInfo.xml", info.ToString());
        Write($".sonarqube/out/{index}/Issues.json", "{}");
        Write($".sonarqube/out/{index}/Telemetry.json", "{}");
        Write($".sonarqube/conf/{index}/FilesToAnalyze.txt", Path.Combine(this.repository, source) + "\n/outside/dependency.cs\n");
    }

    private void AddCoverage(string test, string source)
    {
        var path = Path.Combine(this.coverage, test, "net10.0/coverage.cobertura.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        XElement CoveredClass(string filename, int hits) => new("class", new XAttribute("filename", filename),
            new XElement("lines", new XElement("line", new XAttribute("number", 1), new XAttribute("hits", hits))));
        var doc = new XElement("coverage", new XElement("sources", new XElement("source", "/")),
            new XElement("packages", new XElement("package", new XElement("classes",
                CoveredClass(Path.Combine(this.repository, source), 3), CoveredClass("/outside/dependency.cs", 0)))));
        doc.Save(path);
    }

    private void Write(string relative, string contents)
    {
        var path = Path.Combine(this.repository, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }
}
