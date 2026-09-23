namespace GitVersion.Sonar.Tests;

[TestFixture]
public class WriteTreeTests
{
    private string repository = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.repository = Directory.CreateTempSubdirectory("sonar-git-tree-tests-").FullName;
        await Git("init", ".");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(this.repository, true);

    [Test]
    public async Task WriteTreePreservesBinaryBlobsSkipsSymlinksAndRunsNoCheckoutFiltersOrHooks()
    {
        var bytes = new byte[] { 0, 255, 128, 13, 10, 1, 2, 0, 254 };
        var binary = Path.Combine(this.repository, "payload.bin");
        var attributes = Path.Combine(this.repository, ".gitattributes");
        var link = Path.Combine(this.repository, "linked.bin");
        await File.WriteAllBytesAsync(binary, bytes);
        await File.WriteAllTextAsync(attributes, "payload.bin filter=canary\n");
        File.CreateSymbolicLink(link, "payload.bin");
        var revision = await Commit();
        var filterCanary = Path.Combine(this.repository, "filter-ran");
        var hookCanary = Path.Combine(this.repository, "hook-ran");
        await Git("config", "filter.canary.smudge", $"touch '{filterCanary}'; cat");
        await Git("config", "filter.canary.required", "true");
        var hook = Path.Combine(this.repository, ".git/hooks/post-checkout");
        await File.WriteAllTextAsync(hook, $"#!/bin/sh\ntouch '{hookCanary}'\n");
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(hook, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        File.Delete(binary);
        File.Delete(attributes);
        File.Delete(link);

        await Processes.WriteTree(this.repository, revision);

        Assert.That(await File.ReadAllBytesAsync(binary), Is.EqualTo(bytes));
        Assert.That(await File.ReadAllTextAsync(attributes), Is.EqualTo("payload.bin filter=canary\n"));
        Assert.That(File.Exists(link), Is.False);
        Assert.That(new FileInfo(link).LinkTarget, Is.Null);
        Assert.That(File.Exists(filterCanary), Is.False, "Reading git blobs must not invoke the configured smudge filter.");
        Assert.That(File.Exists(hookCanary), Is.False, "Materializing sources must not invoke post-checkout hooks.");
        Assert.That((await Git("rev-parse", "HEAD")).Trim(), Is.EqualTo(revision));
    }

    [TestCase(".sonarqube")]
    [TestCase(".SonarQube")]
    public async Task WriteTreeRejectsReservedScannerPaths(string directory)
    {
        var reserved = Path.Combine(this.repository, directory);
        Directory.CreateDirectory(reserved);
        var file = Path.Combine(reserved, "attacker.xml");
        await File.WriteAllTextAsync(file, "<attacker/>");
        var revision = await Commit();
        File.Delete(file);
        Directory.Delete(reserved);

        Assert.That(async () => await Processes.WriteTree(this.repository, revision), Throws.TypeOf<InvalidDataException>().With.Message.Contains("Reserved source path"));
        Assert.That(Directory.Exists(reserved), Is.False);
    }

    private Task<string> Git(params string[] arguments) => Processes.Run("/usr/bin/git", arguments, this.repository);

    private async Task<string> Commit()
    {
        await Git("add", "--all");
        await Git("-c", "user.name=Sonar Tests", "-c", "user.email=sonar-tests@example.invalid", "-c", "commit.gpgsign=false", "commit", "--no-verify", "-m", "Fixture source objects");
        return (await Git("rev-parse", "HEAD")).Trim();
    }
}
