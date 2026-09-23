using System.IO.Compression;
using System.Xml;

namespace GitVersion.Sonar.Tests;

[TestFixture]
public class SafeFilesTests
{
    private string root = null!;

    [SetUp]
    public void SetUp() => this.root = Directory.CreateTempSubdirectory("sonar-files-tests-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(this.root, true);

    [TestCase("file.xml")]
    [TestCase("src/Library/File.cs")]
    [TestCase("directory with spaces/report.xml")]
    public void RelativeAcceptsRepositoryPaths(string value) =>
        Assert.That(SafeFiles.Relative(value), Is.EqualTo(value));

    [TestCase("")]
    [TestCase("../escape")]
    [TestCase("dir/../escape")]
    [TestCase("/rooted")]
    [TestCase("C:/rooted")]
    [TestCase("dir\\file")]
    [TestCase("file:stream")]
    [TestCase("dir//file")]
    [TestCase("./file")]
    public void RelativeRejectsAmbiguousOrEscapingPaths(string value) =>
        Assert.That(() => SafeFiles.Relative(value), Throws.TypeOf<InvalidDataException>());

    [Test]
    public void ResolveReturnsContainedFile()
    {
        var expected = Path.Combine(this.root, "reports", "coverage.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(expected)!);
        File.WriteAllText(expected, "<coverage/>");
        Assert.That(SafeFiles.Resolve(this.root, "reports/coverage.xml"), Is.EqualTo(expected));
    }

    [Test]
    public void ResolveRejectsSymbolicLinkAncestor()
    {
        var outside = Directory.CreateDirectory(Path.Combine(this.root, "outside")).FullName;
        var inside = Directory.CreateDirectory(Path.Combine(this.root, "inside")).FullName;
        File.WriteAllText(Path.Combine(outside, "report.xml"), "<coverage/>");
        Directory.CreateSymbolicLink(Path.Combine(inside, "linked"), outside);
        Assert.That(() => SafeFiles.Resolve(inside, "linked/report.xml"), Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void ResolveRejectsTraversal() =>
        Assert.That(() => SafeFiles.Resolve(this.root, "../outside.xml"), Throws.TypeOf<InvalidDataException>());

    [Test]
    public void ReadXmlParsesOrdinaryDocument()
    {
        var path = Path.Combine(this.root, "report.xml");
        File.WriteAllText(path, "<coverage><line hits=\"2\" /></coverage>");
        var document = SafeFiles.ReadXml(path);
        Assert.That(document.Root?.Name.LocalName, Is.EqualTo("coverage"));
        Assert.That(document.Root?.Element("line")?.Attribute("hits")?.Value, Is.EqualTo("2"));
    }

    [TestCase("<!DOCTYPE coverage [<!ENTITY secret SYSTEM 'file:///etc/passwd'>]><coverage>&secret;</coverage>")]
    [TestCase("<!DOCTYPE coverage [<!ENTITY item 'expanded'>]><coverage>&item;</coverage>")]
    [TestCase("<!DOCTYPE coverage><coverage />")]
    [TestCase("<coverage>")]
    public void ReadXmlRejectsEntityDeclarationsAndMalformedDocuments(string value)
    {
        var path = Path.Combine(this.root, "report.xml");
        File.WriteAllText(path, value);
        Assert.That(() => SafeFiles.ReadXml(path), Throws.TypeOf<XmlException>());
    }

    [Test]
    public void UnpackExtractsRegularFiles()
    {
        var zip = CreateArchive(("reports/a.xml", "<a/>"), ("manifest.json", "{}"));
        var destination = Path.Combine(this.root, "extracted");
        SafeFiles.Unpack(zip, destination);
        Assert.That(File.ReadAllText(Path.Combine(destination, "reports", "a.xml")), Is.EqualTo("<a/>"));
        Assert.That(File.ReadAllText(Path.Combine(destination, "manifest.json")), Is.EqualTo("{}"));
        Assert.That(Directory.GetFiles(destination, "*", SearchOption.AllDirectories), Has.Length.EqualTo(2));
    }

    [TestCase("../escape.xml")]
    [TestCase("/rooted.xml")]
    [TestCase("folder\\escape.xml")]
    [TestCase("folder/../../escape.xml")]
    public void UnpackRejectsUnsafeEntriesBeforeWriting(string name)
    {
        var zip = CreateArchive(("valid.xml", "<valid/>"), (name, "<invalid/>"));
        var destination = Path.Combine(this.root, "extracted");
        Assert.That(() => SafeFiles.Unpack(zip, destination), Throws.TypeOf<InvalidDataException>());
        Assert.That(Directory.Exists(destination), Is.False, "The archive must be fully validated before any extraction.");
        Assert.That(File.Exists(Path.Combine(this.root, "escape.xml")), Is.False);
    }

    [Test]
    public void UnpackAcceptsExplicitParentDirectory()
    {
        var zip = CreateArchive(("reports/", ""), ("reports/coverage.xml", "<coverage/>"));
        var destination = Path.Combine(this.root, "extracted");
        SafeFiles.Unpack(zip, destination);
        Assert.That(File.ReadAllText(Path.Combine(destination, "reports", "coverage.xml")), Is.EqualTo("<coverage/>"));
    }

    [TestCase("same.xml")]
    [TestCase("SAME.XML")]
    public void UnpackRejectsDuplicateEntries(string duplicate)
    {
        var zip = CreateArchive(("same.xml", "first"), (duplicate, "second"));
        var destination = Path.Combine(this.root, "extracted");
        Assert.That(() => SafeFiles.Unpack(zip, destination), Throws.TypeOf<InvalidDataException>());
        Assert.That(Directory.Exists(destination), Is.False);
    }

    [Test]
    public void UnpackRejectsSymbolicLinks()
    {
        var zip = Path.Combine(this.root, "archive.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("linked.xml");
            entry.ExternalAttributes = unchecked((int)0xA1FF0000);
            using var writer = new StreamWriter(entry.Open());
            writer.Write("../outside.xml");
        }
        var destination = Path.Combine(this.root, "extracted");
        Assert.That(() => SafeFiles.Unpack(zip, destination), Throws.TypeOf<InvalidDataException>());
        Assert.That(Directory.Exists(destination), Is.False);
    }

    [Test]
    public void UnpackRefusesExistingDestination()
    {
        var zip = CreateArchive(("existing.xml", "replacement"));
        var destination = Directory.CreateDirectory(Path.Combine(this.root, "extracted")).FullName;
        var existing = Path.Combine(destination, "existing.xml");
        File.WriteAllText(existing, "original");
        Assert.That(() => SafeFiles.Unpack(zip, destination), Throws.TypeOf<InvalidDataException>());
        Assert.That(File.ReadAllText(existing), Is.EqualTo("original"));
    }

    [Test]
    public void ReadXmlRejectsOversizedFile()
    {
        var path = Path.Combine(this.root, "oversize.xml");
        using (var file = File.Create(path)) file.SetLength(SafeFiles.MaxFileBytes + 1);
        Assert.That(() => SafeFiles.ReadXml(path), Throws.TypeOf<InvalidDataException>());
    }

    [TestCase(true)]
    [TestCase(false)]
    public void UnpackRejectsFileDirectoryCollisionsBeforeWriting(bool parentFirst)
    {
        var entries = new[] { ("parent", "file"), ("parent/child.xml", "<child/>") };
        var zip = CreateArchive(parentFirst ? entries : entries.Reverse().ToArray());
        var destination = Path.Combine(this.root, "extracted");
        Assert.That(() => SafeFiles.Unpack(zip, destination), Throws.TypeOf<InvalidDataException>());
        Assert.That(Directory.Exists(destination), Is.False);
    }

    [Test]
    public void UnpackRejectsOversizedEntryBeforeWriting()
    {
        var zip = Path.Combine(this.root, "archive.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        using (var output = archive.CreateEntry("oversized.xml", CompressionLevel.Fastest).Open())
        {
            var block = new byte[1024 * 1024];
            for (var i = 0; i < SafeFiles.MaxFileBytes / block.Length; i++) output.Write(block);
            output.WriteByte(0);
        }
        var destination = Path.Combine(this.root, "extracted");
        Assert.That(() => SafeFiles.Unpack(zip, destination), Throws.TypeOf<InvalidDataException>());
        Assert.That(Directory.Exists(destination), Is.False);
    }

    [Test]
    public void FilesReturnsSortedRegularFilesAndAcceptsExactFileSizeLimit()
    {
        var first = Path.Combine(this.root, "a.xml");
        var last = Path.Combine(this.root, "z.xml");
        File.WriteAllText(last, "<z/>");
        using (var file = File.Create(first)) file.SetLength(SafeFiles.MaxFileBytes);
        Assert.That(SafeFiles.Files(this.root), Is.EqualTo(new[] { first, last }));
    }

    [Test]
    public void FilesReturnsEmptyArrayForEmptyDirectory() =>
        Assert.That(SafeFiles.Files(this.root), Is.Empty);

    [Test]
    public void FilesRejectsSymbolicLinkWithoutFollowingIt()
    {
        File.CreateSymbolicLink(Path.Combine(this.root, "dangling.xml"), Path.Combine(this.root, "missing.xml"));
        Assert.That(() => SafeFiles.Files(this.root), Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void FilesRejectsTotalSizeOverLimit()
    {
        for (var i = 0; i <= SafeFiles.MaxTotalBytes / SafeFiles.MaxFileBytes; i++)
        {
            using var file = File.Create(Path.Combine(this.root, $"{i}.xml"));
            file.SetLength(SafeFiles.MaxFileBytes);
        }
        Assert.That(() => SafeFiles.Files(this.root), Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void UnpackRejectsEmptyArchive()
    {
        var zip = CreateArchive();
        var destination = Path.Combine(this.root, "extracted");
        Assert.That(() => SafeFiles.Unpack(zip, destination), Throws.TypeOf<InvalidDataException>());
        Assert.That(Directory.Exists(destination), Is.False);
    }

    [Test]
    public void UnpackRejectsTooManyEntriesBeforeWriting()
    {
        var zip = Path.Combine(this.root, "archive.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            for (var i = 0; i <= SafeFiles.MaxFiles; i++) archive.CreateEntry($"{i}.xml");
        var destination = Path.Combine(this.root, "extracted");
        Assert.That(() => SafeFiles.Unpack(zip, destination), Throws.TypeOf<InvalidDataException>());
        Assert.That(Directory.Exists(destination), Is.False);
    }

    private string CreateArchive(params (string Name, string Content)[] entries)
    {
        var path = Path.Combine(this.root, "archive.zip");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
        return path;
    }
}
